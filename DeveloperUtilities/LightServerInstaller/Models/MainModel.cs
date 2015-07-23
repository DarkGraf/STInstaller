using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

using LightServerLib.Common;
using LightServerLib.Models;

namespace LightServerInstaller.Models
{
  class MainModel : NotifyObject
  {
    public MainModel()
    {
      Messages = new ObservableCollection<string>();
      Messages.CollectionChanged += delegate { NotifyPropertyChanged("Messages"); };
    }

    public Product Product { get; private set; }

    public ObservableCollection<string> Messages { get; private set; }

    public void CreateProduct()
    {
      Product = new Product();
      NotifyPropertyChanged("Product");
    }

    public void LoadFromFile(string fileName)
    {
      Product = Product.Load(fileName);
      NotifyPropertyChanged("Product");
      Messages.Clear();
    }

    public void SaveToFile(string fileName)
    {
      Product.Save(fileName);
      NotifyPropertyChanged("Product");
    }

    public void Build()
    {
      Messages.Clear();

      if (!CheckData())
        return;

      IProcessRunner runner = new ProcessRunner(new Process());
      MsiBuilder builder = new MsiBuilder(Product, runner);
      builder.Build();
      foreach (string str in builder.LastOutput)
        Messages.Add(str);
    }

    /// <summary>
    /// Проверить корректность данных.
    /// </summary>
    /// <returns></returns>
    private bool CheckData()
    {
      // Результат функции будет положителен, если нет сообщений об ошибке.
      if (string.IsNullOrWhiteSpace(Product.Name))
        Messages.Add("Наименование продукта не определено.");
      if (string.IsNullOrWhiteSpace(Product.Manufacturer))
        Messages.Add("Разработчик продукта не определен.");
      if (string.IsNullOrWhiteSpace(Product.Version))
        Messages.Add("Версия продукта не определена.");

      if (!File.Exists(Product.MdfFile))
        Messages.Add("Файл *.mdf не найден.");
      if (!File.Exists(Product.LdfFile))
        Messages.Add("Файл *.ldf не найден.");
      if (!File.Exists(Product.SpDllFile))
        Messages.Add("Файл *.dll хранимой процедуры не найден.");
      if (!File.Exists(Product.SpIniFile))
        Messages.Add("Файл *.ini не найден.");
      if (!File.Exists(Product.SpSqlFile))
        Messages.Add("Файл *.sql хранимой процедуры не найден.");
      if (!File.Exists(Product.SqlFile))
        Messages.Add("Файл *.sql основных скриптов не найден.");
      if (!File.Exists(Product.PluginDllFile))
        Messages.Add("Файл *.dll плагинов не найден.");

      return Messages.Count == 0;
    }
  }
}
