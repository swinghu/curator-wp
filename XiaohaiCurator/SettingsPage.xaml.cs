using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using Windows.Phone.System.UserProfile;
using System.IO.IsolatedStorage;
using XiaohaiCurator.Resources;

namespace XiaohaiCurator
{
  public partial class SettingsPage : PhoneApplicationPage
  {
    private IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

    public SettingsPage()
    {
      InitializeComponent();

    }

    private async void LockScreenSettingButton_Click(object sender, RoutedEventArgs e)
    {
      await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      string tab = "";

      if (NavigationContext.QueryString.TryGetValue("tab", out tab))
      {
        switch (tab)
        {
          case "lockscreen":
            MainPivot.SelectedIndex = 1;
            break;
          default:
            break;
        }
      }

      PeriodicalSwitch.IsEnabled = LockScreenManager.IsProvidedByCurrentApplication;

      base.OnNavigatedTo(e);
    }

    private void PeriodicalSwitch_Checked(object sender, RoutedEventArgs e)
    {
      PeriodicalSwitch.Content = AppResources.SettingsPageLockScreenPeriodicalSwitchContentChecked;

      appSettings[Constant.SETTINGS_IS_PERIODICALLY_UPDATE] = true;
      appSettings.Save();
    }

    private void PeriodicalSwitch_Unchecked(object sender, RoutedEventArgs e)
    {
      PeriodicalSwitch.Content = AppResources.SettingsPageLockScreenPeriodicalSwitchContentUnchecked;
      appSettings[Constant.SETTINGS_IS_PERIODICALLY_UPDATE] = false;
      appSettings.Save();
    }

    private void ShareLinkMessage_TextChanged(object sender, TextChangedEventArgs e)
    {
      appSettings[Constant.SETTINGS_SHARE_LINK_MESSAGE] = ShareLinkMessage.Text;
      appSettings.Save();
    }
  }
}