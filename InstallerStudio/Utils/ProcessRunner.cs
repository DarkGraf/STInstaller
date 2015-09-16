using System;
using System.Diagnostics;
using System.Text;

using InstallerStudio.WixElements.WixBuilders;

namespace InstallerStudio.Utils
{
  class ProcessRunner : IProcessRunner
  {
    private Process process;

    public ProcessRunner(Process process)
    {
      this.process = process;

      // Не создавать окно.
      process.StartInfo.CreateNoWindow = true;
      // Отключаем использование оболочки, чтобы можно было читать данные вывода.
      process.StartInfo.UseShellExecute = false;
      // Перенаправление данных вывода.
      process.StartInfo.RedirectStandardOutput = true;
      process.StartInfo.RedirectStandardError = true;
      // Задаем кириллицу.
      process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);

      process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => 
      { 
        OnOutputMessageReceived(e.Data);
      };
      process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => 
      {
        OnOutputMessageReceived(e.Data);
      };
    }

    private void OnOutputMessageReceived(string message)
    {
      // Делаем потокобезопасно на всякий случай.
      var v = OutputMessageReceived;
      if (v != null && !string.IsNullOrWhiteSpace(message))
        v(this, new ProcessRunnerEventArgs(message));
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
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();
      process.WaitForExit();

      if (HasError = process.ExitCode != 0)
        OnOutputMessageReceived("Код ошибки: " + process.ExitCode.ToString());
      process.Close();
    }

    public bool HasError { get; private set; }

    public event EventHandler<ProcessRunnerEventArgs> OutputMessageReceived;

    #endregion
  }
}
