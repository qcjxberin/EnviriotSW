﻿///<remarks>This file is part of the <see cref="https://github.com/enviriot">Enviriot</see> project.<remarks>
using JSL = NiL.JS.BaseLibrary;
using JSC = NiL.JS.Core;
using JSF = NiL.JS.Core.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using X13.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace X13.UI {
  public partial class InspectorForm : UserControl, IBaseForm {
    private static SortedList<string, Func<InBase, JSC.JSValue, IValueEditor>> _editors;
    static InspectorForm() {
      _editors = new SortedList<string, Func<InBase, JSC.JSValue, IValueEditor>>();
      _editors["Attribute"] = veAttribute.Create;
      _editors["Boolean"] = veSliderBool.Create;
      _editors["ByteArray"] = veByteArray.Create;
      _editors["Double"] = veDouble.Create;
      _editors["Editor"] = veEditor.Create;
      _editors["Enum"] = veEnum.Create;
      _editors["Integer"] = veInteger.Create;
      _editors["JS"] = veJS.Create;
      _editors["Hexadecimal"] = veHexadecimal.Create;
      _editors["String"] = veString.Create;
      _editors["Date"] = veDateTimePicker.Create;
      _editors["TopicReference"] = veTopicReference.Create;
      _editors["Version"] = veVersion.Create;
    }
    public static IValueEditor GetEditor(string editor, InBase owner, JSC.JSValue manifest) {
      IValueEditor rez;
      Func<InBase, JSC.JSValue, IValueEditor> ct;
      if(editor!=null && _editors.TryGetValue(editor, out ct) && ct != null) {
        rez = ct(owner, manifest);
      } else {
        rez = new veDefault(owner, manifest);
      }
      return rez;
    }
    public static IList<string> GetEditors() {
      return _editors.Keys;
    }
    private DTopic _data;
    private ObservableCollection<InBase> _valueVC;

    internal InspectorForm(DTopic data) {
      this._data = data;
      _valueVC = new ObservableCollection<InBase>();
      CollectionChange(new InValue(data, CollectionChange), true);
      CollectionChange(new InManifest(data, CollectionChange), true);
      CollectionChange(new InTopic(data, null, CollectionChange), true);
      InitializeComponent();
      lvValue.ItemsSource = _valueVC;
    }
    private void CollectionChange(InBase item, bool visible) {
      if(item == null) {
        throw new ArgumentNullException("item");
      }
      if(visible) {
        lock(_valueVC) {
          int min = 0, mid = -1, max = _valueVC.Count - 1, cr;

          while(min <= max) {
            mid = (min + max) / 2;
            cr = item.CompareTo(_valueVC[mid]);
            if(cr > 0) {
              min = mid + 1;
            } else if(cr < 0) {
              max = mid - 1;
              mid = max;
            } else {
              break;
            }
          }
          _valueVC.Insert(mid + 1, item);
        }
      } else {
        _valueVC.Remove(item);
      }
    }

    private void ListViewItem_KeyUp(object sender, KeyEventArgs e) {
      var gr = e.OriginalSource as ListViewItem;
      if(gr != null) {
        var it = gr.DataContext as InBase;
        if(e.Key == Key.Apps || e.Key == Key.Space) {
          if(gr.ContextMenu != null && it != null) {
            gr.ContextMenu.ItemsSource = it.MenuItems(gr);
            gr.ContextMenu.IsOpen = true;
          }
          return;
        }
        if(e.Key == Key.Left || e.Key == Key.Right) {
          if(it != null) {
            if(e.Key == Key.Right && it.HasChildren && !it.IsExpanded) {
              it.IsExpanded = true;
              e.Handled = true;
            } else if(e.Key == Key.Left) {
              if(it.IsExpanded) {
                it.IsExpanded = false;
              } else {
                base.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Up));
              }
              e.Handled = true;
            }
          }
        }
      }
    }
    private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
      FrameworkElement gr;
      InBase d;
      if((gr = sender as FrameworkElement) != null && (d = gr.DataContext as InBase) != null) {
        gr.ContextMenu.ItemsSource = d.MenuItems(gr);
        return;
      }
      e.Handled = true;
    }
    private void ListViewItem_ContextMenuClosing(object sender, ContextMenuEventArgs e) {
      var gr = sender as FrameworkElement;
      if(gr != null && gr.ContextMenu != null) {
        gr.ContextMenu.ItemsSource = null;
        gr.ContextMenu.Items.Clear();
      }
    }
    private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e) {
      ListViewItem gr;
      InBase it;
      if((gr = sender as ListViewItem) != null && (it = gr.DataContext as InBase) != null) {
        var mis = it.MenuItems(gr);
        MenuItem mi;
        if(mis != null && (mi = mis.OfType<MenuItem>().FirstOrDefault(z => z.Header as string == "Open in new tab")) != null) {
          mi.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
      }
    }
    private void tbItemName_Loaded(object sender, RoutedEventArgs e) {
      (sender as TextBox).SelectAll();
      (sender as TextBox).Focus();
    }
    private void tbItemName_PreviewKeyDown(object sender, KeyEventArgs e) {
      TextBox tb;
      InBase tv;
      if((tb = sender as TextBox) == null || (tv = tb.DataContext as InBase) == null) {
        return;
      }
      if(e.Key == Key.Escape) {
        tv.FinishNameEdit(null);
        e.Handled = true;
      } else if(e.Key == Key.Enter) {
        tv.FinishNameEdit(tb.Text);
        e.Handled = true;
      }
    }
    private void tbItemName_LostFocus(object sender, RoutedEventArgs e) {
      TextBox tb;
      InTopic tv;
      if((tb = sender as TextBox) == null || (tv = tb.DataContext as InTopic) == null) {
        return;
      }
      tv.FinishNameEdit(tb.Text);
      e.Handled = true;
    }

    #region IBaseForm Members
    public string view {
      get { return "Inspector"; }
    }
    public BitmapSource icon { get { return App.GetIcon(null); } }
    public bool altView {
      get { return false; }
      //get { return _data != null && (_data.typeStr == "Logram"); }
    }
    #endregion IBaseForm Members
  }
  internal class GridColumnSpringConverter : IMultiValueConverter {
    public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
      double v = values.OfType<double>().Aggregate((x, y) => x -= y) - 26;
      return v > 0 ? v : 100;
    }
    public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
      throw new System.NotImplementedException();
    }
  }
}
