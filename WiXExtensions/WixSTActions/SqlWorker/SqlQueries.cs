using System;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Запросы.
  /// </summary>
  // ВНИМАНИЕ: все запросы должны сохранять подключение к той базе данных, которая была активна до выполнения запросов.
  // Кроме запросов присоединения, отсоединения и переключения баз данных.
  static class SqlQueries
  {
    /// <summary>
    /// Проверка у текущего пользователя административных прав.
    /// </summary>
    // Используется динамический SQL чтобы не изменить подключение к текущей базе,
    // на базу "master", так как в exec использование use не переключает базу в текущем
    // соединении.
    public const string IsAdmin = "exec ('use master select is_member (''db_owner'')')";

    /// <summary>
    /// Получение версии сервера.
    /// </summary>
    public const string ServerVersion =
@"declare @version nvarchar(128)
declare @name nvarchar(128)

set @version = cast(serverproperty('ProductVersion') as nvarchar)
set @version = substring(@version, 1, charindex('.', @version) - 1)

if (@version = '7')
  set @name = 'SQL Server 7'
else if (@version = '8')
  set @name = 'SQL Server 2000'
else if (@version = '9')
  set @name = 'SQL Server 2005'
else if (@version = '10')
  set @name = 'SQL Server 2008/2008 R2'
else if (@version = '11')
  set @name = 'SQL Server 2012'
else
  set @name = 'Unknow SQL Server Version'

select @version as VersionCode, @name as VersionName";

    /// <summary>
    /// Получение списка баз данных из специальной процедуры.
    /// Входная переменная: @Version (varchar(19)).
    /// </summary>
    public const string GetDatabaseFromProcedure =
@"if (object_id('SP_STVerifier', 'P') is not null)
begin
  select 1
  exec master..SP_STVerifier @Version
end
else
  select 0";

    /// <summary>
    /// Получение идентификатора сервиса.
    /// </summary>
    public const string GetPid = "select ServerProperty('ProcessID') as Pid";

    /// <summary>
    /// Присоединение базы данных.
    /// Вход: 
    /// {0} - имя базы данных.
    /// {1} - путь к mdf-файлу.
    /// {2} - путь к ldf-файлу.
    /// Выход:
    /// 0 - успешно.
    /// 1 - не успешно.
    /// </summary>
    public const string AttachDatabase =
@"if db_id('{0}') is not null
begin
  select 1
  return
end

create database [{0}]
on
  (filename = '{1}'), 
  (filename = '{2}') 
for attach

if db_id('{0}') is not null
  select 0
else
  select 1";

    /// <summary>
    /// Отсоедиение базы данных.
    /// Вход:
    /// {0} - имя базы данных.
    /// </summary>
    public const string DetachDatabase =
@"use master
if db_id('{0}') is not null
begin
  exec sp_detach_db [{0}], 'true'
end";

    /// <summary>
    /// Получение путей к физическим файлам базы данных.
    /// </summary>
    public const string GetPhysicalFilePath = "select physical_name as path, type_desc as type from sys.database_files";

    /// <summary>
    /// Установка однопользовательского режима базы данных.
    /// Вход:
    /// {0} - имя базы данных.
    /// </summary>
    public const string SetSingleUser = "alter database [{0}] set single_user with rollback immediate";

    /// <summary>
    /// Установка многопользовательского режима базы данных.
    /// Вход:
    /// {0} - имя базы данных.
    /// </summary>
    public const string SetMultiUser = "alter database [{0}] set multi_user";

    /// <summary>
    /// Переключение баз данных.
    /// Вход:
    /// {0} - имя базы данных.
    /// </summary>
    public const string UseDatabase = "use [{0}]";
  }
}
