﻿///<remarks>This file is part of the <see cref="https://github.com/enviriot">Enviriot</see> project.<remarks>
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace X13.UI {
  /// <summary></summary>
  public partial class UIDocument : UserControl, INotifyPropertyChanged {
    private ObservableCollection<DTopic> _pathItems;
    private string _path;
    private string _view;
    private IBaseForm _contentForm;

    public UIDocument(string path, string view) {
      _path = path;
      _view = view;
      _pathItems = new ObservableCollection<DTopic>();
      this.DataContext = this;
      InitializeComponent();
      this.icPanel.ItemsSource = _pathItems;
      Uri url;
      if(path != null && Uri.TryCreate(path, UriKind.Absolute, out url)) {
        RequestData(url);
      }
    }

    private DTopic _data;

    public bool connected { get { return _data != null; } }
    public DTopic data { get { return _data; } }
    public IBaseForm contentForm {
      get {
        return _contentForm;
      }
      set {
        if(_contentForm != value) {
          _contentForm = value;
          OnPropertyChanged("contentForm");
        }
      }
    }
    public string ContentId { get { return (_data == null ? _path : _data.fullPath) + (_view == null ? string.Empty : ("?view=" + _view)); } }

    private void RequestData(Uri url) {
      this.Cursor = Cursors.AppStarting;
      App.Workspace.GetAsync(url).ContinueWith(this.DataUpd, TaskScheduler.FromCurrentSynchronizationContext());
    }
    private void DataUpd(Task<DTopic> t) {
      if(t.IsFaulted) {
        Log.Warning("{0}", t.Exception.Message);
      } else if(t.IsCompleted) {
        _data = t.Result;
        if(_data == null) {  // topic deleted
          App.Workspace.Close(this);
          return;
        }
        _path = _data.fullPath;
        OnPropertyChanged("data");

        DTopic c = _data;
        _pathItems.Clear();
        while(c != null) {
          _pathItems.Insert(0, c);
          c = c.parent;
        }
        if(_view == null) {
          //if(_data.typeStr == "Logram") {
          //  _view = "LO";
          //} else {
            _view = "IN";
          //}
        }
        OnPropertyChanged("ContentId");
        OnPropertyChanged("connected");
        if(_view == "IN") {
          if((ccMain.Content as InspectorForm) == null) {
            contentForm = new InspectorForm(_data);
          }

        //} else if(_view == "LO") {
        //  if((ccMain.Content as LogramForm) == null) {
        //    contentForm = new LogramForm(_data);
        //  }

        }
      }
      this.Focus();
      this.Cursor = Cursors.Arrow;
    }

    #region Address bar
    private void tbAddress_Loaded(object sender, RoutedEventArgs e) {
      if(!this.connected && _path == null) {
        tbAddress.Focus();
      }
    }
    private void TextBox_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e) {
      if((bool)e.NewValue) {
        this.icPanel.Visibility = System.Windows.Visibility.Collapsed;
      } else if(connected) {
        this.icPanel.Visibility = System.Windows.Visibility.Visible;
      } else {
        tbAddress.Focus();
      }

    }
    private void TextBox_KeyUp(object sender, KeyEventArgs e) {
      if(e.Key == Key.Enter) {
        Uri url;
        if(Uri.TryCreate(tbAddress.Text, UriKind.Absolute, out url)) {
          tbAddress.Background = null;
          RequestData(url);
        } else {
          tbAddress.Background = Brushes.LightPink;
        }
      } else if(e.Key == Key.Escape) {
        App.Workspace.Close(this);
      }
    }
    private void Button_Click(object sender, RoutedEventArgs e) {
      var bu = sender as Button;
      DTopic t;
      if(bu != null && (t = bu.DataContext as DTopic) != null) {
        App.Workspace.Open(t.fullPath);
      }
    }
    #endregion Address bar

    private void buChangeView_Click(object sender, RoutedEventArgs e) {
      //if((ccMain.Content as InspectorForm) != null) {
      //  if(_data.typeStr == "Logram") {
      //    _view = "LO";
      //    contentForm = new LogramForm(_data);
      //    OnPropertyChanged("ContentId");
      //  }
      //} else {
        //_view = "IN";
        //contentForm = new InspectorForm(_data);
        //OnPropertyChanged("ContentId");
      //}
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;

    internal void OnPropertyChanged(string propertyName) {
      if(PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion INotifyPropertyChanged Members

  }
}
