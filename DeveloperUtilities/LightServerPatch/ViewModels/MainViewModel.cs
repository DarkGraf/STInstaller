using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

using LightServerLib.Commands;
using LightServerLib.Common;
using LightServerPatch.Models;

namespace LightServerPatch.ViewModels
{
  interface IMainView
  {
    void ShowAndLockMessageWindow();
    void UnlockMessageWindow();
    void AddMessage(string message, bool isError);
  }

  interface IMainViewModelListener
  {
    IMainView View { get; set; }
  }

  class MainViewModel : NotifyObject, IMainViewModelListener
  {
    private MainModel mainModel;
    private string defaultPath;

    public ICommand FindCommand { get; private set; }
    public ICommand BuildCommand { get; private set; }
    public ICommand SelectDirectoryCommand { get; private set; }

    #region Свойства для интерфейса.

    public string CurrentWixout 
    {
      get { return mainModel.PatchInfo.CurrentWixout; }
      set { mainModel.PatchInfo.CurrentWixout = value; }
    }

    public string CurrentXml
    {
      get { return mainModel.PatchInfo.CurrentXml; }
      set { mainModel.PatchInfo.CurrentXml = value; }
    }

    public string NewWixout
    {
      get { return mainModel.PatchInfo.NewWixout; }
      set { mainModel.PatchInfo.NewWixout = value; }
    }

    public string NewXml
    {
      get { return mainModel.PatchInfo.NewXml; }
      set { mainModel.PatchInfo.NewXml = value; }
    }

    public string OutDirectory
    {
      get { return mainModel.PatchInfo.OutDirectory; }
      set { mainModel.PatchInfo.OutDirectory = value; }
    }

    #endregion

    public MainViewModel()
    {
      mainModel = new MainModel();
      mainModel.SendMessage += mainModel_MessageSend;

      defaultPath = Environment.CurrentDirectory;

      FindCommand = new RelayCommand(param => Find(param));
      BuildCommand = new RelayCommand(param => Build(),
        delegate
        {
          return !string.IsNullOrWhiteSpace(CurrentWixout) && !string.IsNullOrWhiteSpace(CurrentXml)
            && !string.IsNullOrWhiteSpace(NewWixout) && !string.IsNullOrWhiteSpace(NewXml); 
        });
      SelectDirectoryCommand = new RelayCommand(param => SelectDirectory());
    }    

    private void Find(object param)
    {
      if (!(param is string))
        return;

      string extension;

      new Dictionary<string, string>
      {
        { "CurrentWixout", "*.wixout" },
        { "CurrentXml", "*.xml" },
        { "NewWixout", "*.wixout" },
        { "NewXml", "*.xml" }
      }.TryGetValue((string)param, out extension);

      if (extension == null)
        return;

      PropertyInfo prop = GetType().GetProperty((string)param);
      string path = (string)prop.GetValue(this, null);

      OpenFileDialog dialog = new OpenFileDialog();
      dialog.InitialDirectory = string.IsNullOrWhiteSpace(path) ? defaultPath : Path.GetDirectoryName(path);
      // Если путь был относительный, то преобразуется в абсолютный.
      dialog.InitialDirectory = Path.GetFullPath(dialog.InitialDirectory);
      dialog.Filter = string.Format("{0}|{0}|Все файлы|*.*", extension);
      if (dialog.ShowDialog() == true)
      {
        // Преобразуем абсолютный путь в относительный.
        Uri appPath = new Uri(AppDomain.CurrentDomain.BaseDirectory);
        Uri filePath = new Uri(dialog.FileName);
        string relativePath = appPath.MakeRelativeUri(filePath).ToString();

        prop.SetValue(this, relativePath);
        defaultPath = Path.GetDirectoryName(dialog.FileName);
        NotifyPropertyChanged(prop.Name);
      }
    }

    private void Build()
    {
      Task.Factory.StartNew(delegate
      {
        View.AddMessage("Построение начато...", false);
        View.ShowAndLockMessageWindow();
        mainModel.Build();
        View.UnlockMessageWindow();
        View.AddMessage("", false);
        View.AddMessage("Построение окончено...", false);
      });
    }

    private void SelectDirectory()
    {
      System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

      dialog.SelectedPath = string.IsNullOrWhiteSpace(OutDirectory) ? defaultPath : OutDirectory;
      // Если путь был относительный, то преобразуется в абсолютный.
      dialog.SelectedPath = Path.GetFullPath(dialog.SelectedPath);
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        // Преобразуем абсолютный путь в относительный.
        Uri appPath = new Uri(AppDomain.CurrentDomain.BaseDirectory);
        Uri selectedPath = new Uri(dialog.SelectedPath);
        string relativePath = appPath.MakeRelativeUri(selectedPath).ToString();

        OutDirectory = relativePath;
        defaultPath = dialog.SelectedPath;
        NotifyPropertyChanged("OutDirectory");
      }
    }

    void mainModel_MessageSend(object sender, MainModelEventArgs e)
    {
      View.AddMessage("", false);
      View.AddMessage(e.MessageSource, false);
      View.AddMessage(e.Message, e.Type == MainModelMessageType.Error);
    }

    #region IMainViewModelListener

    public IMainView View { get; set; }

    #endregion
  }
}
