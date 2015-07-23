using System;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixWidget
{
  class WixWidgetQuery
  {
    internal const string queryGetAllWidgets = "SELECT * FROM Control";

    // Type не входит в ключ, но для подстраховки правильной типизации используем.
    /// <summary>
    /// Получение свойств виджета.
    /// Параметры: имя диалога, имя виджета, тип виджета.
    /// </summary>
    internal const string queryGetWidget =
      "SELECT * FROM Control WHERE Control.Dialog_ = '{0}' AND Control.Control = '{1}' AND Control.Type = '{2}'";

    internal const string queryGetAllCondition = "SELECT * FROM ControlCondition";
  }

  enum WixWidgetContditionType
  {
    Default, // Set control as the default.
    Disable, // Disable the control.
    Enable, // Enable the control.
    Hide, // Hide the control.
    Show // Display the control.
  }

  /// <summary>
  /// Абстрактный базовый класс для динамического создания элементов управления WIX.
  /// </summary>
  abstract class WixWidget<T> : IWixWidgetPropertiesAccess
    where T : WixControl.WixControl
  {
    const uint attrVisible = 0x01;
    const uint attrEnabled = 0x02;
    const uint attrSunken = 0x04;

    WixWidgetProperty<int> x;
    WixWidgetProperty<int> y;
    WixWidgetProperty<int> width;
    WixWidgetProperty<int> height;
    WixWidgetProperty<int> attributes;
    WixWidgetProperty<string> property;
    WixWidgetProperty<string> text;
    WixWidgetProperty<string> controlNext;
    WixWidgetProperty<string> help;

    public Session Session { get; private set; }

    /// <summary>
    /// Создание объекта для доступа к виджету.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="dialog">Имя диалога</param>
    /// <param name="name">Имя виджета.</param>
    /// <param name="type">Тип виджета.</param>
    /// <param name="isNew">Истино, если нужно создать новый виджет, ложно если виджет существует.</param>
    public WixWidget(Session session, string dialog, string name, string type)
    {
      Session = session;
      Dialog = dialog;
      Name = name;
      Type = type;

      // Просто описание свойств, значения здесь не получаем.
      this.x = new WixWidgetProperty<int>("X", this);
      this.y = new WixWidgetProperty<int>("Y", this);
      this.width = new WixWidgetProperty<int>("Width", this);
      this.height = new WixWidgetProperty<int>("Height", this);
      this.attributes = new WixWidgetProperty<int>("Attributes", this);
      this.property = new WixWidgetProperty<string>("Property", this);
      this.text = new WixWidgetProperty<string>("Text", this);
      this.controlNext = new WixWidgetProperty<string>("Control_Next", this);
      this.help = new WixWidgetProperty<string>("Help", this);

      // Создаем виджет и указываем атрибуты Visible и Enable.
      CreateWidget();
    }

    #region Открытые свойства.

    public string Dialog { get; private set; }

    public string Name { get; private set; }

    public string Type { get; private set; }

    public int X 
    {
      get { return x.Value; }
      set { x.Value = value; }
    }

    public int Y
    {
      get { return y.Value; }
      set { y.Value = value; }
    }
    
    public int Width
    {
      get { return width.Value; }
      set { width.Value = value; }
    }

    public int Height
    {
      get { return height.Value; }
      set { height.Value = value; }
    }

    public string Property
    {
      get { return property.Value; }
      set { property.Value = value; }
    }

    public string Text
    {
      get { return text.Value; }
      set { text.Value = value; }
    }

    public string ControlNext
    {
      get { return controlNext.Value; }
      set { controlNext.Value = value; }
    }

    public string Help
    {
      get { return help.Value; }
      set { help.Value = value; }
    }

    #endregion

    #region Свойства атрибутов.

    public bool Enabled
    {
      get { return GetAttributes(attrEnabled); }
      set { SetAttributes(attrEnabled, value); }
    }

    public bool Visible
    {
      get { return GetAttributes(attrVisible); }
      set { SetAttributes(attrVisible, value); }
    }

    public bool Sunken
    {
      get { return GetAttributes(attrSunken); }
      set { SetAttributes(attrSunken, value); }
    }

    #endregion

    /// <summary>
    /// Дополнительные атрибуты, каждый виджет расшифровывает индивидуально.
    /// </summary>
    private uint Attributes
    {
      get { return (uint)attributes.Value; }
      set { attributes.Value = (int)value; }
    }

    protected bool GetAttributes(uint attr)
    {
      return (Attributes & attr) != 0;
    }

    protected void SetAttributes(uint attr, bool val)
    {
      Attributes = val ? Attributes | attr : Attributes & (~attr);
    }

    protected void CreateWidget()
    {
      using (View view = Session.Database.OpenView(WixWidgetQuery.queryGetAllWidgets))
      {
        // Пишем в таблицу Control.
        view.Execute();
        Record record = Session.Database.CreateRecord(12);
        record.SetString(1, Dialog);
        record.SetString(2, Name);
        record.SetString(3, Type);
        record.SetInteger(4, 0);
        record.SetInteger(5, 0);
        record.SetInteger(6, 0);
        record.SetInteger(7, 0);
        record.SetInteger(8, 0);
        record.SetString(9, null);
        record.SetString(10, null);
        record.SetString(11, null);
        record.SetString(12, null);
        view.Modify(ViewModifyMode.InsertTemporary, record);
      }

      Enabled = true;
      Visible = true;
    }

    /// <summary>
    /// Возвращает объект для манипуляции с данными.
    /// </summary>
    public abstract T Control { get; }

    /// <summary>
    /// Управление состоянием виджета.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="condition"></param>
    public void AddCondition(WixWidgetContditionType type, string condition)
    {
      using (View view = Session.Database.OpenView(WixWidgetQuery.queryGetAllCondition))
      {
        view.Execute();
        Record record = Session.Database.CreateRecord(4);
        record.SetString(1, Dialog);
        record.SetString(2, Name);
        record.SetString(3, Enum.GetName(typeof(WixWidgetContditionType), type));
        record.SetString(4, condition);
        view.Modify(ViewModifyMode.InsertTemporary, record);
      }
    }
  }
}
