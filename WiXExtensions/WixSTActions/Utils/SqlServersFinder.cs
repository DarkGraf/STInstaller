using System;
using Microsoft.Win32;

namespace WixSTActions.Utils
{
    static class SqlServersFinder
    {
        public static string[] Find()
        {
            // Получаем список локальных серверов из реестра.
            string[] instances;
#warning Найти определения типа windows.
            if (Environment.Is64BitProcess || true)
            {
                RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
                instances = (string[])key.GetValue("InstalledInstances");
            }
            else
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
                instances = (string[])key.GetValue("InstalledInstances");
            }

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
