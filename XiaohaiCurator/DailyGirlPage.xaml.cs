using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using XiaohaiCurator.ViewModels;

namespace XiaohaiCurator
{
  public partial class DailyGirlPage : PhoneApplicationPage
  {
    private string name;
    private string day;

    public DailyGirlPage()
    {
      InitializeComponent();

      DataContext = App.ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      var qs = NavigationContext.QueryString;

      if (qs.TryGetValue("name", out name) && qs.TryGetValue("day", out day))
      {
        Name.Text = name;
        Day.Text = day;
        App.ViewModel.LoadGirlOfDay(day);
      }
    }

    private void LongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      GirlViewModel item = GirlOfTodayList.SelectedItem as GirlViewModel;

      if (null != item)
      {
        int index = App.ViewModel.GirlOfToday.IndexOf(item);

        NavigationService.Navigate(new Uri(string.Format("/PhotoViewerPage.xaml?type=day&index={0}", index), UriKind.Relative));
        GirlOfTodayList.SelectedItem = null;
      }

    }
  }
}