using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixControl
{
  class WixEdit : WixControl
  {    
    public WixEdit(Session session, string property) : base(session, property) 
    {
      Text = new ObservableCollection<string>();
      ((ObservableCollection<string>)Text).CollectionChanged += WixEdit_CollectionChanged;
    }

    void WixEdit_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      // Если Edit со включенным свойством Multiline, 
      // то он отображает только первое значение.
      string str = string.Empty;
      foreach (string s in Text)
        str += str.Length == 0 ? s : (Environment.NewLine + s);
      Session[Property] = str;
    }

    public IList<string> Text { get; private set; }
  }
}
