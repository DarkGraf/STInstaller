using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixWidget
{
  interface IWixWidgetPropertiesAccess
  {
    Session Session { get; }
    string Dialog { get; }
    string Name { get; }
    string Type { get; }
  }

  /// <summary>
  /// Свойства виджетов. Считывает свойство только при первом обращении к нему,
  /// затем кеширует. При изменении, меняет свойство в базе и у себя.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  class WixWidgetProperty<T>
  {
    string name;
    IWixWidgetPropertiesAccess widget;
    bool hasValue;
    T value;

    public WixWidgetProperty(string name, IWixWidgetPropertiesAccess widget)
    {
      this.name = name;
      this.widget = widget;
      
      hasValue = false;
      value = default(T);
    }

    public T Value 
    {
      get { return GetValue(); }
      set { SetValue(value); }
    }

    private T GetValue()
    {
      if (!hasValue)
      {
        using (View view = GetView())
        {
          view.Execute();
          using (Record record = view.Fetch())
          {
            if (record != null)
            {
              value = (T)record[name];
              hasValue = true;
            }
          }
        }
      }
      return value;
    }

    private void SetValue(T value)
    {
      if (hasValue && this.value.Equals(value))
        return;
          
      using (View view = GetView())
      {
        view.Execute();
        using (Record record = view.Fetch())
        {
          if (record != null)
          {
            record[name] = value;
            view.Modify(ViewModifyMode.Update, record);

            this.value = value;
            hasValue = true;
          }
        }
      }
    }

    private View GetView()
    {
      return widget.Session.Database.OpenView(WixWidgetQuery.queryGetWidget, widget.Dialog, widget.Name, widget.Type);
    }
  }
}
