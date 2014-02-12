#define DEBUG

using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using XiaohaiCurator.Resources;
using XiaohaiCurator.ViewModels;

namespace XiaohaiCurator
{
  public partial class MainPage : PhoneApplicationPage
  {
    private PeriodicTask periodicTask;
    private const string periodicTaskName = "XiaohaiCuratorAgent";
    private int page = 0;
    private bool isStreamLoaded = false;

    private const int magicOffset = 9;

    // Constructor
    public MainPage()
    {
      InitializeComponent();

      // try to start the background agent.
      StartPeriodicAgent();

      // Set the data context of the listbox control to the sample data
      DataContext = App.ViewModel;

      // For infinite scrolling
      GirlsStreamList.ItemRealized += GirlsStreamList_ItemRealized;
    }

    void GirlsStreamList_ItemRealized(object sender, ItemRealizationEventArgs e)
    {
      // try to tell if user has scrolled to the end of list
      if (!App.ViewModel.IsStreamEnd && !App.ViewModel.IsLoading && 
          GirlsStreamList.ItemsSource != null && GirlsStreamList.ItemsSource.Count >= magicOffset)
      {
        if (e.ItemKind == LongListSelectorItemKind.Item)
        {
          if ((e.Container.Content as GirlViewModel).Equals(GirlsStreamList.ItemsSource[GirlsStreamList.ItemsSource.Count - magicOffset]))
          {
            App.ViewModel.LoadStream(++page);
          }
        }
      }
    }

    /// <summary>
    /// Start the background task for change the lockscreen image.
    /// </summary>
    private void StartPeriodicAgent()
    {
      periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;
      if (periodicTask != null)
      {
        try
        {
          ScheduledActionService.Remove(periodicTaskName);
        }
        catch (Exception)
        {
        }
      }

      // setup a new periodic task.
      periodicTask = new PeriodicTask(periodicTaskName);
      periodicTask.Description = AppResources.BackgroundTaskDescription;
      periodicTask.ExpirationTime = DateTime.Now.AddDays(14);

      try
      {
        ScheduledActionService.Add(periodicTask);
#if DEBUG
        ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
        Debug.WriteLine("Task" + periodicTaskName + " is launched");
#endif
      }
      catch (InvalidOperationException)
      {
        // FIXME:
      }
      catch (SchedulerServiceException)
      {
        // FIXME:
      }
    }

    // Load data for the ViewModel Items
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      if (!App.ViewModel.IsDataLoaded)
      {
        App.ViewModel.LoadData();
      }
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // load girl stream if the stream has not been loaded.
      if (MainPivot.SelectedIndex == 1 && !isStreamLoaded)
      {
        App.ViewModel.LoadStream(++page);
        isStreamLoaded = true;
      }
    }

    private void GirlsStreamList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      GirlViewModel item = GirlsStreamList.SelectedItem as GirlViewModel;

      if (null != item)
      {
        int index = App.ViewModel.GirlsStream.IndexOf(item);

        NavigationService.Navigate(new Uri("/PhotoViewerPage.xaml?type=stream&index=" + index, UriKind.Relative));
        GirlsStreamList.SelectedItem = null;
      }
    }

    private void DailyGirlsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      GirlViewModel item = DailyGirlsList.SelectedItem as GirlViewModel;
      if (null != item)
      {
        NavigationService.Navigate(
          new Uri(string.Format("/DailyGirlPage.xaml?name={0}&day={1}", item.Name, item.DateAt), UriKind.Relative));
        DailyGirlsList.SelectedItem = null;
      }
    }
  }
}