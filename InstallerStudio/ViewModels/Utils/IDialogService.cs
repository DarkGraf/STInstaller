using System;

namespace InstallerStudio.ViewModels.Utils
{
  /// <summary>
  /// Сервис диалогов. Содержит интерфейсы для вызова различных диалогов.
  /// Предназначен для реализации во View.
  /// </summary>
  interface IDialogService
  {
    IOpenSaveFileDialog OpenFileDialog { get; }
    IOpenSaveFileDialog SaveFileDialog { get; }
    ISettingsDialog SettingsDialog { get;  }
  }

  /// <summary>
  /// Предназначен для реализации в View Model, для установки сервиса диалогов.
  /// </summary>
  interface IDialogServiceSetter
  {
    IDialogService DialogService { get; set; }
  }

  /// <summary>
  /// Диалог открытия/сохранения файлов.
  /// </summary>
  interface IOpenSaveFileDialog
  {
    string Filter { get; set; }

    int FilterIndex { get; set; }

    string Title { get; set; }

    string FileName { get; set; }

    bool? Show();
  }

  /// <summary>
  /// Диалог настроек.
  /// </summary>
  interface ISettingsDialog
  {
    string WixToolsetPath { get; set; }
    string CandleFileName { get; set; }
    string LightFileName { get; set; }
    string UIExtensionFileName { get; set; }

    bool? Show();
  }
}
