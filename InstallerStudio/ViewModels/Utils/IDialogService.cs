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
    IMspWizardDialog MspWizardDialog { get; }
    IMessageBoxDialog MessageBoxInfo { get; }
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
    string TorchFileName { get; set; }
    string PyroFileName { get; set; }
    string UIExtensionFileName { get; set; }
    string SuppressIce { get; set; }

    bool? Show();
  }

  public enum MspWizardContentTypes { AllInOne, EachInOne, Empty }

  /// <summary>
  /// Диалог получения начальных параметров для создания модели Msp.
  /// </summary>
  interface IMspWizardDialog
  {
    string PathToBaseSource { get; set; }
    string PathToTargetSource { get; set; }
    MspWizardContentTypes ContentType { get; set; }

    bool? Show();
  }

  public enum MessageBoxDialogTypes { None, Error, Question, Exclamation, Information }

  interface IMessageBoxDialog
  {
    MessageBoxDialogTypes Type { get; set; }
    string Message { get; set; }
    /// <summary>
    /// Показать окно.
    /// </summary>
    /// <returns>Истино если да, ложно если нет, null если отмена.</returns>
    bool? Show();
  }
}
