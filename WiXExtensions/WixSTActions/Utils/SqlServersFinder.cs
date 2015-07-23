using System;
using Microsoft.Win32;

namespace WixSTActions.Utils
{
  static class SqlServersFinder
  {
    public static string[] Find()
    {
      // Получаем список локальных серверов из реестра.
      RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
      String[] instances = (String[])key.GetValue("InstalledInstances");

      if (instances == null)
        instances = new string[0];

      for (int i = 0; i < instances.Length; i++)
      {
        if (instances[i] == "MSSQLSERVER")
          instances[i] = Environment.MachineName;
        else
          instances[i] = Environment.MachineName + "\\" + instances[i];
      }
      
      return instances;
    }
  }
}
