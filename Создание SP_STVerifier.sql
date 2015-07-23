use master
go

if (object_id('SP_STVerifier', 'P') is not null)
  drop procedure SP_STVerifier
go

create procedure SP_STVerifier @installVersion varchar(19)
as
  -- Описание подпроцедуры приведения версии.
  declare @subProcedureVersionFormatParams nvarchar(100)
  declare @subProcedureVersionFormat nvarchar(4000)
  set @subProcedureVersionFormatParams = '@isNew bit, @ver varchar(19) OUTPUT'
  set @subProcedureVersionFormat = 
  'declare @i int
  -- Приводим входную версию к общему формату.
  set @i = 0;

  set @ver = rtrim(@ver)

  -- Может прийти только одна цифра, т.е. основная версия.
  while @i < 3 and @ver not like ''%.%.%.%''
  begin
    set @ver = @ver + ''.0''
    set @i = @i + 1
  end

  -- Если не удалось добавить три точки, выходим.
  if @ver not like ''%.%.%.%''
  begin
    -- Если версия для устанавливаемой базы, то берем минимум,
	-- иначе максимум.
	if @isNew = 1
      set @ver = ''0.0.0.0''
	else
	  set @ver = ''255.255.65536.65536''
    return
  end
   
  --  1234567890123456789
  --  255.255.65536.65536 (19 символов).
  while charindex(''.'', @ver, 1) < 4
    set @ver = stuff(@ver, 1, 0, ''0'')
  while charindex(''.'', @ver, 5) < 8
    set @ver = stuff(@ver, 5, 0, ''0'')
  while charindex(''.'', @ver, 9) < 14
    set @ver = stuff(@ver, 9, 0, ''0'')
  while len(@ver) < 19
    set @ver = stuff(@ver, 15, 0, ''0'')'

  declare @name nvarchar(128)
  declare @command nvarchar(1000)
  declare @count int

  if (object_id('tempdb..#dbVersions') is not null)
    drop table #dbVersions
  create table #dbVersions
  (
    Name nvarchar(128),
    OldVersion nvarchar(19),
    FullOldVersion nvarchar(50),
	NewVersion nvarchar(19),
    FullNewVersion nvarchar(50)
  )

  -- Курсор содержит все базы данных на текущем сервере.
  -- Должен быть static, иначе если запустить подряд сразу нескольно
  -- раз эту процедуру, то выдаст неправильные значения.
  declare cur cursor static for 
    select name from master..sysdatabases

  open cur

  fetch next from cur into @name
  while @@FETCH_STATUS <> -1
  begin
    -- Определяем базу данных.
    -- Если есть таблица vDBVersions, то считаем что база наша.
    set @command = N'if object_id (@n + ''.dbo.vDBVersions'') is not null select @cnt = 1 else select @cnt = 0'
	exec sp_executesql @command, N'@n nvarchar(128), @cnt int OUTPUT', @n = @name, @cnt = @count OUTPUT

	if @count = 1
	begin
	  exec('use ' + @name + ' insert into #dbVersions (Name, FullOldVersion) 
	  select db_name(), Description from vDBVersions where DefinitionSqlPref is not null')
	end
    
    fetch next from cur into @name
  end

  close cur
  deallocate cur

  -- Убираем лишние символы из версии.
  declare @pattern as varchar(50)
  set @pattern = '%[0-9]%.[0-9]%.[0-9]%.[0-9]%'
  
  -- В начале.
  update #dbVersions
  set
    FullOldVersion = substring(FullOldVersion, patindex(@pattern, FullOldVersion), len(FullOldVersion) - patindex(@pattern, FullOldVersion) + 1)
  where patindex(@pattern, FullOldVersion) > 1

  -- В конце.
  set @pattern = '%[^0-9.]%'
  update #dbVersions
  set
    FullOldVersion = substring(FullOldVersion, 1, patindex(@pattern, FullOldVersion) - 1)
  from #dbVersions
  where patindex(@pattern, FullOldVersion) > 0

  -- Минимальная версия для инсталлятора.
  declare @minVersion as int = 900
  
  ;with FormattedVersions as 
  (
    select *, 
	  case
        when RevisionVersion - @minVersion < 0 then '0.9.0'
	    else '1.0.' + cast(RevisionVersion - @minVersion as varchar(5)) + '.0'
	  end as Num 
	from (select *, cast(replace(FullOldVersion, '2.7.0.', '') as int) as RevisionVersion from #dbVersions) as tbl
  )

  -- Запомним полученные версии.
  update #dbVersions
  set
    OldVersion = #dbVersions.FullOldVersion,
    FullNewVersion = Num,
    NewVersion = Num
  from #dbVersions
    inner join FormattedVersions on
	  #dbVersions.Name = FormattedVersions.Name
	  and #dbVersions.FullOldVersion = FormattedVersions.FullOldVersion
  
  -- Каждую версию преобразовываем в общий формат.
  declare @fullOldVersion nvarchar(50)
  declare @fullNewVersion nvarchar(50)

  declare cur cursor for 
    select FullOldVersion, FullNewVersion from #dbVersions

  open cur

  fetch next from cur into @fullOldVersion, @fullNewVersion
  while @@FETCH_STATUS <> -1
  begin
    exec sp_executesql @subProcedureVersionFormat, @subProcedureVersionFormatParams, @isNew = 0, @ver = @fullOldVersion OUTPUT
	exec sp_executesql @subProcedureVersionFormat, @subProcedureVersionFormatParams, @isNew = 0, @ver = @fullNewVersion OUTPUT
	update #dbVersions 
	set 
	  FullOldVersion = @fullOldVersion,
	  FullNewVersion = @fullNewVersion
	where current of cur

    fetch next from cur into @fullOldVersion, @fullNewVersion
  end

  close cur
  deallocate cur

  -- Приводим версию из входных данных к общему формату.
  exec sp_executesql @subProcedureVersionFormat, @subProcedureVersionFormatParams, @isNew = 1, @ver = @installVersion OUTPUT

  -- Выбираем максимальную версию для каждой базы и возвращаем результат.
  select
    grp.Name,
	NewVersion as Version,
	cast(case
	  when @installVersion > FullNewVersion then 1
	  else 0
	end as bit) as IsRequiringUpdate,
	-- Поля для отладки.
	OldVersion,
    grp.FullOldVersion,
	NewVersion,
	FullNewVersion,	
	@installVersion as InstallVersion
  from
    (select Name, max(FullOldVersion) as FullOldVersion from #dbVersions group by Name) as grp
	inner join #dbVersions on
	  grp.Name = #dbVersions.Name
	  and grp.FullOldVersion = #dbVersions.FullOldVersion
  order by grp.Name
go