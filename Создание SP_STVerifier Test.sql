use master
go

if (object_id('SP_STVerifier', 'P') is not null)
  drop procedure SP_STVerifier
go

create procedure SP_STVerifier @installVersion varchar(19)
as
  declare @command nvarchar(1000)
  declare @table nvarchar(100)
  declare @count int

  declare @name nvarchar(128)
  declare @version nvarchar(19)
  declare @print_version nvarchar(19)
  declare @isRequiringUpdate bit

  declare @i int

  declare @tbl table
  (
    Name nvarchar(128),
    -- Максимальное значение версии 255.255.65536.65536 (19 символов).
    Version nvarchar(19),
    IsRequiringUpdate bit
  )

  set @installVersion = rtrim(@installVersion)

  -- Приводем входную версию к общему формату.
  set @i = 0;

  -- Может прийти только одна цифра, т.е. основная версия.
  while @i < 3 and @installVersion not like '%.%.%.%'
  begin
    set @installVersion = @installVersion + '.0'
    set @i = @i + 1
  end

  -- Если не удалось добавить три точки.
  if @installVersion not like '%.%.%.%'
  begin
    select * from  @tbl
    return
  end
   
  --  1234567890123456789
  --  255.255.65536.65536 (19 символов).
  while charindex('.', @installVersion, 1) < 4
    set @installVersion = stuff(@installVersion, 1, 0, '0')
  while charindex('.', @installVersion, 5) < 8
    set @installVersion = stuff(@installVersion, 5, 0, '0')
  while charindex('.', @installVersion, 9) < 14
    set @installVersion = stuff(@installVersion, 9, 0, '0')
  while len(@installVersion) < 19
    set @installVersion = stuff(@installVersion, 15, 0, '0')

  -- Курсор содержит все базы данных на текущем сервере.
  -- Должен быть static, иначе если запустить подряд сразу нескольно
  -- раз эту процедуру, то выдаст неправильные значения.
  declare cur cursor static for 
    select name from master..sysdatabases

  open cur

  fetch next from cur into @name
  while @@FETCH_STATUS <> -1
  begin
    set @version = '255.255.65536.65536'
    -- Определяем базу данных.
    -- Если есть заданная таблица, то считаем что база наша.
    set @command = N'if object_id (''' + @name + '.dbo.Table_1'') is not null select @cnt = 1 else select @cnt = 0'
    exec sp_executesql @command, N'@cnt int OUTPUT', @cnt = @count OUTPUT
	if @count = 1
	begin
	  set @version = '001.000.00000.00000'
	  set @print_version = '1.0.0.0'
	end

	set @command = replace(@command, 'Table_1', 'GO11')
	exec sp_executesql @command, N'@cnt int OUTPUT', @cnt = @count OUTPUT
	if @count = 1
	begin
	  set @version = '001.000.00001.00000'
	  set @print_version = '1.0.1.0'
	end

    set @command = replace(@command, 'GO11', 'GO21')
	exec sp_executesql @command, N'@cnt int OUTPUT', @cnt = @count OUTPUT
	if @count = 1
	begin
	  set @version = '001.000.00002.00000'
	  set @print_version = '1.0.2.0'
	end

    set @command = replace(@command, 'GO21', 'GO31')
	exec sp_executesql @command, N'@cnt int OUTPUT', @cnt = @count OUTPUT
	if @count = 1
	begin
	  set @version = '001.000.00003.00000'
	  set @print_version = '1.0.3.0'
	end

	set @command = replace(@command, 'GO31', 'GO41')
	exec sp_executesql @command, N'@cnt int OUTPUT', @cnt = @count OUTPUT
	if @count = 1
	begin
	  set @version = '001.000.00004.00000'
	  set @print_version = '1.0.4.0'
	end

    if @version <> '255.255.65536.65536' 
    begin
	  select @isRequiringUpdate = case when @installVersion > @version then 1 else 0 end
      insert into @tbl (Name, Version, IsRequiringUpdate) values(@name, @print_version, @isRequiringUpdate)
    end

    fetch next from cur into @name
  end

  close cur
  deallocate cur

  select * from @tbl
go