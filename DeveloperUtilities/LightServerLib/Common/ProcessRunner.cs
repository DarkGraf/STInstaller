using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LightServerLib.Common
{
  public interface IProcessRunner
  {
    /// <summary>
    /// Запуск процесса.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="arguments"></param>
    void Start(string fileName, string arguments);

    bool HasError { get; }

    IEnumerable<string> Output { get; }
  }

  public class ProcessRunner : IProcessRunner
  {
    Process process;
    bool hasError = false;
    string output = string.Empty;

    public ProcessRunner(Process process)
    {
      this.process = process;

      // Не создавать окно.
      process.StartInfo.CreateNoWindow = true;
      // Отключаем использование оболочки, чтобы можно было читать данные вывода.
      process.StartInfo.UseShellExecute = false;
      // Перенаправление данных вывода.
      process.StartInfo.RedirectStandardOutput = true;
      // Задаем кириллицу.
      process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
    }

    #region IProcessRunner

    public void Start(string fileName, string arguments)
    {
      // Запускаемый файл.
      process.StartInfo.FileName = fileName;
      // Аргументы к этому файлу.
      process.StartInfo.Arguments = arguments;

      // Запускаем процесс.
      process.Start();
      StreamReader reader = process.StandardOutput;
      process.WaitForExit();

      output = reader.ReadToEnd();
      hasError = process.ExitCode != 0;
    }

    public bool HasError
    {
      get { return hasError; }
    }

    public IEnumerable<string> Output
    {
      get { return output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); }
    }

    #endregion
  }
}
