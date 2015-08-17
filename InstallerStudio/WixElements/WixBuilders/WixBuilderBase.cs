using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Resources;

using InstallerStudio.Utils;

namespace InstallerStudio.WixElements.WixBuilders
{
  // Паттерн "Шаблонный метод".
  abstract class WixBuilderBase : TempFileStoreBase
  {
    private void ToLoadTemplates()
    {
      string[] templateNames = GetTemplateFileNames();

      foreach (string templateName in templateNames)
      {
        string strUri = string.Format("pack://application:,,,/{0};component/{1}",
          Assembly.GetExecutingAssembly().GetName().Name, "WixElements/WixBuilders/" + templateName);
        Uri uri = new Uri(strUri);
        
        StreamResourceInfo resourceInfo = Application.GetResourceStream(uri);

        string fileName = Path.Combine(StoreDirectory, Path.GetFileName(templateName));
        using (FileStream file = new FileStream(fileName, FileMode.CreateNew))
        {
          resourceInfo.Stream.CopyTo(file);
        }
      }
    }

    public void Build()
    {
      // Загружаем шаблоны во временную папку.
      ToLoadTemplates();
    }

    #region Абстрактные защищенные методы и свойства.

    /// <summary>
    /// Получает директорию файлов-шаблонов.
    /// </summary>
    /// <returns></returns>
    protected abstract string[] GetTemplateFileNames();

    #endregion
  }
}
