using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.ObjectModel;
using XiaohaiCurator.ViewModels;

namespace XiaohaiCurator
{
  public partial class PhotoViewerPage : PhoneApplicationPage
  {
    private ObservableCollection<GirlViewModel> collection;
    private int currentIndex;

    public Uri imageUrl { get; private set; }

    public PhotoViewerPage()
    {
      InitializeComponent();

      DataContext = this;
      
      ImageContainer.ManipulationCompleted += ImageContainer_ManipulationCompleted;
      ImageContainer.ImageOpened += ImageContainer_ImageOpened;
    }

    void ImageContainer_ImageOpened(object sender, RoutedEventArgs e)
    {
      SystemTray.ProgressIndicator.IsVisible = false;
    }

    void ImageContainer_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
    {
      var x = e.TotalManipulation.Translation.X;
      var y = e.TotalManipulation.Translation.Y;
      var sx = e.TotalManipulation.Scale.X;
      var sy = e.TotalManipulation.Scale.Y;

      if (Math.Abs(x) > Math.Abs(y) && sx == 0.0 && sy == 0.0)
      {
        if (x > 50.0 && currentIndex > 0)
        {
          ShowPicture(currentIndex - 1);
        }
        else if (x < -50.0 && currentIndex + 1 < collection.Count)
        {
          ShowPicture(currentIndex + 1);
        }
      }

    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      string type, index;
      var qs = NavigationContext.QueryString;

      if (qs.TryGetValue("type", out type) && qs.TryGetValue("index", out index))
      {
        collection = type == "stream" ? App.ViewModel.GirlsStream :
        type == "day" ? App.ViewModel.GirlOfToday : App.ViewModel.DailyGirls;

        ShowPicture(int.Parse(index));
      }
    }

    private void ShowPicture(int index)
    {
      SystemTray.ProgressIndicator.IsVisible = true;
      var item = collection[index];
      BitmapImage bitmap = new BitmapImage(item.ImageUrl);
      ImageContainer.Source = bitmap;
      currentIndex = index;
    }
  }
}