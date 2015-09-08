using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using InstallerStudio.WixElements;

namespace InstallerStudio.Models
{
  /// <summary>
  /// Типы создания содержимого Msp.
  /// </summary>
  enum MspCreationTypes
  {
    /// <summary>
    /// Все компоненты в одном пакете.
    /// </summary>
    AllInOne,
    /// <summary>
    /// Каждый компонент в одном пакете.
    /// </summary>
    EachInOne,
    /// <summary>
    /// Пустой Msp.
    /// </summary>
    Empty
  }

  interface IMspModelLoadingParamters
  {
    string PathToBaseFile { get; }
    string PathToTargetFile { get; }
    MspCreationTypes CreationType { get; }
  }

  /// <summary>
  /// Информация об обновленном компоненте.
  /// </summary>
  [DataContract(Namespace = StringResources.Namespace)]
  class UpdateComponentInfo
  {
    [DataMember]
    public string Id { get; set; }
  }

  class MspModelLoadingParameters : IMspModelLoadingParamters
  {
    public MspModelLoadingParameters(string pathToBaseFile, string pathToTargetFile, MspCreationTypes creationType)
    {
      PathToBaseFile = pathToBaseFile;
      PathToTargetFile = pathToTargetFile;
      CreationType = creationType;
    }

    #region IMspModelParamters

    public string PathToBaseFile { get; private set; }
    public string PathToTargetFile { get; private set; }
    public MspCreationTypes CreationType { get; private set; }

    #endregion
  }

  class MspModel : BuilderModel
  {
    WixPatchProduct mainItem;

    new WixPatchProduct MainItem
    {
      get
      {
        if (mainItem == null)
          mainItem = (WixPatchProduct)base.MainItem;
        return mainItem;
      }
    }

    /// <summary>
    /// Директория для хранения старой версии.
    /// </summary>
    internal const string BaseDirectory = "Base";

    /// <summary>
    /// Директория для хранения новой версии.
    /// </summary>
    internal const string TargetDirectory = "Target";

    public void Load(IMspModelLoadingParamters parameters)
    {
      // BaseDirectory и TargetDirectory используем только в этом методе, при создании.
      // Это позволит в будущем при необходимости изменять эти имена директорий
      // и поддерживать функциональность старых файлов, так как в них записаны старые
      // пути с которыми они создавались.
      string baseFile = BaseDirectory + "\\" + Path.GetFileName(parameters.PathToBaseFile);
      string targetFile = TargetDirectory + "\\" + Path.GetFileName(parameters.PathToTargetFile);

      // Копируем файлы во временную директорию.
      FileStore.AddFile(parameters.PathToBaseFile, baseFile);
      FileStore.AddFile(parameters.PathToTargetFile, targetFile);

      // Распаковываем файлы.
      // Распакуются там, где находится архив.
      WixProductUpdateInfo baseInfo = WixProductUpdateInfo.Load(Path.Combine(FileStore.StoreDirectory, baseFile));
      WixProductUpdateInfo targetInfo = WixProductUpdateInfo.Load(Path.Combine(FileStore.StoreDirectory, targetFile));

      // Сразу запомним обновляемую информацию в Product.
      MainItem.BaseId = baseInfo.Id;
      MainItem.BaseName = baseInfo.Name;
      MainItem.BaseManufacturer = baseInfo.Manufacturer;
      MainItem.BaseVersion = baseInfo.Version;
      MainItem.BasePath = Path.Combine(BaseDirectory, baseInfo.WixoutFileName);
      MainItem.TargetId = targetInfo.Id;
      MainItem.TargetName = targetInfo.Name;
      MainItem.TargetManufacturer = targetInfo.Manufacturer;
      MainItem.TargetVersion = targetInfo.Version;
      MainItem.TargetPath = Path.Combine(TargetDirectory, targetInfo.WixoutFileName);

      // Добавим файлы в хранилище.
      FileStore.AddFile(Path.Combine(FileStore.StoreDirectory, MainItem.BasePath), MainItem.BasePath);
      FileStore.AddFile(Path.Combine(FileStore.StoreDirectory, MainItem.TargetPath), MainItem.TargetPath);

      // Удалим файлы updzip - больше не нужны.
      FileStore.DeleteFile(baseFile);
      FileStore.DeleteFile(targetFile);

      // Патч разрешено выпускать только при добавленных или измененных компонентах.
      // То есть необходимо получить элементы множества targetInfo, которые
      // отсутствуют в множестве baseFile.
      // Получаем только Component и его наследика DbComponent.
      WixMD5ElementHash[] actuals = targetInfo.Hashes.Except(baseInfo.Hashes).
        Where(v => v.Type == typeof(WixComponentElement).Name
        || v.Type == typeof(WixDbComponentElement).Name).ToArray();
      
      var updateComponents = from v in actuals
                             select new UpdateComponentInfo { Id = v.Id };
      // Загружаем обновляемы компоненты в модель.
      foreach (var c in updateComponents)
      {
        MainItem.UpdateComponents.Add(c);
      }

      switch (parameters.CreationType)
      {
        case MspCreationTypes.AllInOne:
          LoadAllInOne();
          break;
        case MspCreationTypes.EachInOne:
          LoadEachInOne();
          break;
        case MspCreationTypes.Empty:
          // Ни чего не делаем.
          break;
      }
    }

    private void LoadAllInOne()
    {
      WixPatchElement patch = new WixPatchElement();
      patch.Id = "FullComponentPatch";
      RootItem.Items.Add(patch);
      foreach (UpdateComponentInfo info in UpdateComponents)
      {
        WixPatchComponentElement component = new WixPatchComponentElement();
        component.Id = info.Id;
        patch.Items.Add(component);
      }
    }

    private void LoadEachInOne()
    {
      foreach (UpdateComponentInfo info in UpdateComponents)
      {
        WixPatchElement patch = new WixPatchElement();
        patch.Id = info.Id + "Patch";
        RootItem.Items.Add(patch);
        WixPatchComponentElement component = new WixPatchComponentElement();
        component.Id = info.Id;
        patch.Items.Add(component);
      }
    }

    /// <summary>
    /// Обновлемые компоненты.
    /// </summary>
    public UpdateComponentInfo[] UpdateComponents 
    { 
      get 
      { 
        return MainItem.UpdateComponents.ToArray(); 
      } 
    }

    #region BuilderModel

    protected override IWixMainEntity CreateMainEntity()
    {
      WixPatchProduct patch = new WixPatchProduct();
      return patch;
    }

    public override CommandMetadata[] GetElementCommands()
    {
      return new CommandMetadata[] { new CommandMetadata("Общие", typeof(WixPatchElement)) };
    }

    #endregion
  }

  class MspModelFactory : BuilderModelFactory
  {
    public override BuilderModel Create()
    {
      return new MspModel();
    }
  }
}
