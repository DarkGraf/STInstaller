using System;
using System.Runtime.Serialization;

using InstallerStudio.Models;
using InstallerStudio.WixElements.WixBuilders;
using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Описывает интерфейс самой главной сущности Wix.
  /// </summary>
  interface IWixMainEntity
  {
    /// <summary>
    /// Корневой элемент сущности Wix.
    /// </summary>
    IWixElement RootElement { get; }

    /// <summary>
    /// Построение целевого объекта.
    /// </summary>
    void Build(IBuildContext buildContext);
  }

  /// <summary>
  /// Контекст для построения.
  /// Содержит информацию необходимую для построения.
  /// </summary>
  interface IBuildContext
  {
    /// <summary>
    /// Вывод сообщений о построении.
    /// </summary>
    void BuildMessageWriteLine(string message);
    /// <summary>
    /// Очистка сообщений о построении.
    /// </summary>
    void ClearBuildMessage();
    /// <summary>
    /// Установки программы.
    /// </summary>
    ISettingsInfo ApplicationSettings { get; }
    /// <summary>
    /// Имя файла загруженного проекта.
    /// </summary>
    string ProjectFileName { get; }
    /// <summary>
    /// Метод вызываемый когда построение закончено.
    /// Внимание. Вызывается с не UI потока.
    /// </summary>
    Action BuildIsFinished { get; }
  }

  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixMainEntity : IWixMainEntity
  {
    protected abstract WixBuilderBase CreateBuilder();

    #region IWixMainEntity

    [DataMember]
    public abstract IWixElement RootElement { get; protected set; }

    public void Build(IBuildContext buildContext)
    {
      // Внимание!!! Не используем конструкцию using, так как
      // внутри объекта происходит асинхронная работа и мы удалим 
      // быстрее чем она завершится. Среда сама удалит объект и 
      // ресурсы после освобождения всех ссылок.
      WixBuilderBase builder = CreateBuilder();
      builder.Build(buildContext);
    }

    #endregion
  }
}
