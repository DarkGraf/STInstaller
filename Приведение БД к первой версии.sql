exec SP_STVerifier '1.0.0.0'
go
declare @tbl table
(
  Name nvarchar(128),
  Version nvarchar(19),
  IsRequiringUpdate bit
)
declare @name nvarchar(128)
declare @command nvarchar(1000)

if (object_id('tempdb..#tmp') is not null)
  drop table #tmp
create table #tmp(Name char(128))

-- Получаем таблицы.

insert into @tbl exec SP_STVerifier '1.0.0.0'

declare cur cursor for 
  select Name from @tbl

open cur
fetch next from cur into @name
while @@FETCH_STATUS <> -1
begin
  set @command = 'use [' + @name + '] insert into #tmp select ''[' + @name + 
    '].dbo.'' + name from sys.tables where name like ''GO__'' or name = ''InstallASPO_XP'''
  exec(@command)

  fetch next from cur into @name
end

close cur
deallocate cur

-- Удаляем таблицы.

declare cur cursor for 
  select Name from #tmp

open cur
fetch next from cur into @name
while @@FETCH_STATUS <> -1
begin
  set @command = ' drop table ' + @name
  exec(@command)

  print('Удалено ' + @name)
  fetch next from cur into @name
end

close cur
deallocate cur
go
exec SP_STVerifier '1.0.0.0'