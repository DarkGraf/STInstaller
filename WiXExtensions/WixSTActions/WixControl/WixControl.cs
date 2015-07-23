using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixControl
{
  /// <summary>
  /// Абстрактный базовый класс для доступа к элементам управления WIX.
  /// Доступ осуществляется для управления данными элемента.
  /// </summary>
  abstract class WixControl
  {
    protected Session Session { get; private set; }
    protected string Property { get; private set; }

    public WixControl(Session session, string property)
    {
      Session = session;
      Property = property;
    }
  }

  abstract class WixContainerControl<T> : WixControl
  {
    public WixContainerControl(Session session, string property) : base(session, property) { }

    public void AddItem(T item)
    {
      using (View view = Session.Database.OpenView(SelectQuery, Property))
      {
        view.Execute();
        // Определяем номер вставки.
        int order = GetMaxOrder(view) + 1;
        InsertRecord(view, item, order);
      }
    }

    public void AddItems(T[] items)
    {
      using (View view = Session.Database.OpenView(SelectQuery, Property))
      {
        view.Execute();
        // Определяем номер вставки.
        int order = GetMaxOrder(view) + 1;
        foreach (T item in items)
        {
          InsertRecord(view, item, order++);
        }
      }
    }

    public void RemoveItem(string value)
    {
      using (View view = Session.Database.OpenView(DeleteQuery, Property, value))
      {
        view.Execute();
      }
    }

    public void ClearItems()
    {
      using (View view = Session.Database.OpenView(ClearQuery, Property))
      {
        view.Execute();
      }
    }

    public ReadOnlyCollection<T> Items
    {
      get
      {
        List<T> items = new List<T>();

        using (View view = Session.Database.OpenView(SelectQuery, Property))
        {
          view.Execute();
          Record record;
          while ((record = view.Fetch()) != null)
          {
            items.Add(CreateItem(record));
            record.Dispose();
          }
        }

        return items.AsReadOnly();
      }
    }

    public string SelectedValue
    {
      get { return Session[Property]; }
      set { Session[Property] = value; }
    }

    /// <summary>
    /// Определяет максимальный Order в таблице.
    /// </summary>
    protected int GetMaxOrder(View view)
    {
      int max = 0;
      Record record;
      while ((record = view.Fetch()) != null)
      {
        // У ListView, ComboBox и ListBox вторая колонка порядок.
        if (max < record.GetInteger(2))
          max = record.GetInteger(2);
        record.Dispose();
      }
      return max;
    }

    /// <summary>
    /// Запрос выборки.
    /// </summary>
    protected abstract string SelectQuery { get; }

    /// <summary>
    /// Запрос удаления.
    /// </summary>
    protected abstract string DeleteQuery { get; }

    /// <summary>
    /// Запрос удаления всего.
    /// </summary>
    protected abstract string ClearQuery { get; }

    /// <summary>
    /// Добавляет один элемент в открытое представление.
    /// </summary>
    protected abstract void InsertRecord(View view, T item, int order);

    /// <summary>
    /// Создает экземпляр элемента из записи базы данных.
    /// </summary>
    protected abstract T CreateItem(Record record);
  }
}
