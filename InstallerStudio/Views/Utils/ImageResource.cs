using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InstallerStudio.Views.Utils
{
  public enum MenusImagesTypes
  {
    [UriImage("Views/Images/NewLarge.png")]
    New,
    [UriImage("Views/Images/CloseLarge.png")]
    Close,
    [UriImage("Views/Images/BuildLarge.png")]
    Build,
    [UriImage("Views/Images/CheckLarge.png")]
    Check,
    [UriImage("Views/Images/OpenLarge.png")]
    Open,
    [UriImage("Views/Images/SaveLarge.png")]
    Save
  }

  public enum ElementsImagesTypes
  {
    [UriImage("Views/Images/Components/FeatureSmall.png")]
    Feature,
    [UriImage("Views/Images/Components/ComponentSmall.png")]
    Component,
    [UriImage("Views/Images/Components/DbComponentSmall.png")]
    DbComponent,
    [UriImage("Views/Images/Components/SqlScriptSmall.png")]
    SqlScript,
    [UriImage("Views/Images/Components/FileSmall.png")]
    File,
    [UriImage("Views/Images/Components/ShortcutSmall.png")]
    Shortcut,
    [UriImage("Views/Images/Components/SqlExtentedProceduresSmall.png")]
    SqlExtentedProcedures,
    [UriImage("Views/Images/Components/MefPluginSmall.png")]
    MefPlugin
  }

  /// <summary>
  /// Аттрибут содержащий абсолютный путь к ресурсу изображения.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class UriImageAttribute : Attribute
  {
    public Uri Uri { get; private set; }

    public UriImageAttribute(string uri)
    {
      Uri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, uri));
    }
  }

  public interface IImageResource
  {
    ImageSource this[Enum index] { get; }
  }

  /// <summary>
  /// Содержит объекты ImageSource с ресурсов соответствующих перечислению T.
  /// </summary>
  // Паттерн "Моносостояние".
  class ImageResource<T> : IImageResource
  {
    private static IDictionary<T, ImageSource> images;

    static ImageResource()
    {
      if (!typeof(T).IsEnum)
        throw new ArgumentException();

      images = new Dictionary<T, ImageSource>();

      // Загружаем все элементы перечисления и их изображения в словарь.
      foreach (var e in Enum.GetValues(typeof(T)))
      {
        UriImageAttribute attr = e.GetType().GetField(e.ToString()).GetCustomAttribute<UriImageAttribute>(false);
        if (attr != null)
          images.Add((T)e, new BitmapImage(attr.Uri));
      }
    }

    #region IImageResource

    public ImageSource this[Enum index]
    {
      get { return images[(T)(object)index]; }
    }

    #endregion
  }

  class MenusImages : ImageResource<MenusImagesTypes> { }

  class ComponentsImages : ImageResource<ElementsImagesTypes> { }

  static class ImageResourceFactory
  {
    public static IImageResource CreateImageResource(Type type)
    {
      if (type == typeof(MenusImagesTypes))
        return new MenusImages();

      if (type == typeof(ElementsImagesTypes))
        return new ComponentsImages();

      throw new ArgumentException();
    }
  }
}
