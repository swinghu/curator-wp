#define DEBUG

using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using XiaohaiCurator.Resources;
using XiaohaiCurator.ViewModels;
using Microsoft.Phone.Shell;
using System.Windows;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Info;

namespace XiaohaiCurator
{
  public partial class MainPage : PhoneApplicationPage
  {
    private PeriodicTask periodicTask;
    private const string periodicTaskName = "XiaohaiCuratorAgent";
    private int page = 0;
    private bool isStreamLoaded = false;

    private const int magicOffset = 9;

    private int currentPivotIndex = 0;
    private int layoutStatus = 0;

    // layout button
    private ApplicationBarIconButton layoutButton;
    // settings menu item
    private ApplicationBarMenuItem settingsMenuItem;

    // pointer to current displayed list
    private LongListSelector focusedList;

    // Constructor
    public MainPage()
    {
      InitializeComponent();

      if (!App.IsLowMemoryDevice)
      {
        TiltEffect.SetIsTiltEnabled(this, true);
      }

      // try to start the background agent.
      StartPeriodicAgent();

      // Init localized app bar
      BuildLocalizedApplicationBar();

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
    /// Build localized app bar for the main page.
    /// </summary>
    private void BuildLocalizedApplicationBar()
    {
      ApplicationBar = new ApplicationBar();

      // create the layout button
      layoutButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/layout.list.png", UriKind.Relative)) 
        { 
          Text = AppResources.LayoutButtonListText
        };
      layoutButton.Click += (s, e) =>
        {
          if (currentPivotIndex == 0)
          {
            layoutStatus += (layoutStatus & 1) == 0 ? 1 : -1;
          }
          else
          {
            layoutStatus += ((layoutStatus >> 1) & 1) == 0 ? 2 : -2;
          }

          if (layoutButton.Text == AppResources.LayoutButtonListText)
          {
            ToggleLayoutButton(1);
            focusedList.LayoutMode = LongListSelectorLayoutMode.List;
            focusedList.ItemTemplate = Resources["ListImagesTemplate"] as DataTemplate;
          }
          else
          {
            ToggleLayoutButton(0);
            focusedList.LayoutMode = LongListSelectorLayoutMode.Grid;
            focusedList.ItemTemplate = Resources["TiledImagesTemplate"] as DataTemplate;
          }
        };

      settingsMenuItem = new ApplicationBarMenuItem(AppResources.MainPageAppBarSettings);
      settingsMenuItem.Click += (s, e) =>
        {
          NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        };

      ApplicationBar.Buttons.Add(layoutButton);
      ApplicationBar.MenuItems.Add(settingsMenuItem);
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

      if (NavigationContext.QueryString.ContainsKey("streamType"))
      {
        string streamType = NavigationContext.QueryString["streamType"];
        Debug.WriteLine(streamType);
        if (streamType.Equals("正咩流"))
        {
          MainPivot.SelectedIndex = 1;
        }
      }

      base.OnNavigatedTo(e);
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      currentPivotIndex = MainPivot.SelectedIndex;

      if (currentPivotIndex == 0)
      {
        focusedList = DailyGirlsList;

        ToggleLayoutButton(layoutStatus & 1);
      }
      else if (currentPivotIndex == 1)
      {
        focusedList = GirlsStreamList;

        ToggleLayoutButton((layoutStatus >> 1) & 1);

        // load girl stream if the stream has not been loaded.
        if (!isStreamLoaded)
        {
          App.ViewModel.LoadStream(++page);
          isStreamLoaded = true;
          Debug.WriteLine(DeviceStatus.ApplicationCurrentMemoryUsage + " / " + DeviceStatus.ApplicationMemoryUsageLimit);

        }
      }
    }

    /// <summary>
    /// Switch Layout Button from List (0) to Grid (1)
    /// </summary>
    /// <param name="mode">Layout mode. 0 for list; 1 for grid.</param>
    private void ToggleLayoutButton(int mode)
    {
      if (mode == 0)
      {
        layoutButton.IconUri = new Uri("/Assets/AppBar/layout.list.png", UriKind.Relative);
        layoutButton.Text = AppResources.LayoutButtonListText;
      }
      else
      {
        layoutButton.IconUri = new Uri("/Assets/AppBar/layout.grid.png", UriKind.Relative);
        layoutButton.Text = AppResources.LayoutButtonGridText;
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

    private void MainPivot_LoadedPivotItem(object sender, PivotItemEventArgs e)
    {
      var loadItem = e.Item;
      if (loadItem == null)
      {
        return;
      }
      ClearItem(loadItem);
    }

    private void ClearItem(PivotItem pivotItem)
    {
      foreach (var item in MainPivot.Items)
      {
        var pi = item as PivotItem;
        if (pi != pivotItem)
        {
          (pi.Content as UIElement).Visibility = Visibility.Collapsed;
        }
        else
        {
          (pi.Content as UIElement).Visibility = Visibility.Visible;
        }
      }
    }
  }
}