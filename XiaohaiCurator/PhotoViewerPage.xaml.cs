﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using XiaohaiCurator.Resources;
using XiaohaiCurator.ViewModels;
using Windows.Phone.System.UserProfile;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace XiaohaiCurator
{
  public partial class PhotoViewerPage : PhoneApplicationPage
  {
    private ObservableCollection<GirlViewModel> collection;
    private int currentIndex;
    private IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

    public string imageUrl { get; private set; }

    // download/save button
    private ApplicationBarIconButton saveButton;
    // set as lockscreen photo
    private ApplicationBarIconButton lockscreenButton;
    // share button
    private ApplicationBarIconButton shareButton;

    public PhotoViewerPage()
    {
      InitializeComponent();

      DataContext = this;

      BuildLocalizedApplicationBar();
      
      ImageContainer.ManipulationCompleted += ImageContainer_ManipulationCompleted;
      ImageContainer.ImageOpened += ImageContainer_ImageOpened;
      ImageContainer.ImageFailed += ImageContainer_ImageFailed;
    }

    /// <summary>
    /// Handle if the image contain failed to load the image.
    /// </summary>
    void ImageContainer_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
      MessageBox.Show(AppResources.NetworkError, AppResources.NetworkErrorCaption, MessageBoxButton.OK);
    }

    /// <summary>
    /// Build localized app bar for the photo viewer page.
    /// </summary>
    private void BuildLocalizedApplicationBar()
    {
      ApplicationBar = new ApplicationBar();
      ApplicationBar.Mode = ApplicationBarMode.Minimized;

      saveButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/download.png", UriKind.Relative)) 
        { 
          Text = AppResources.SaveButtonText
        };
      saveButton.Click += saveButton_Click;

      lockscreenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/favs.png", UriKind.Relative)) 
        {
          Text = AppResources.LockscreenButtonText
        };
      lockscreenButton.Click += lockscreenButton_Click;

      // share the page
      shareButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/share.png", UriKind.Relative)) 
        {
          Text = AppResources.PhotoViewerPageAppBarShare
        };
      shareButton.Click += shareButton_Click;

      ApplicationBar.Buttons.Add(saveButton);
      ApplicationBar.Buttons.Add(shareButton);
      ApplicationBar.Buttons.Add(lockscreenButton);
    }

    /// <summary>
    /// Handle the share button is clicked.
    /// </summary>
    void shareButton_Click(object sender, EventArgs args)
    {
      var item = collection[currentIndex];

      ShareLinkTask shareLinkTask = new ShareLinkTask();
      shareLinkTask.Title = string.Format("小海嚴選 - {0}", item.Name);
      shareLinkTask.LinkUri = new Uri(string.Format("http://curator.im/item/{0}/", item.Id), UriKind.Absolute);
      if (appSettings.Contains(Constant.SETTINGS_SHARE_LINK_MESSAGE))
      {
        shareLinkTask.Message = (string)appSettings[Constant.SETTINGS_SHARE_LINK_MESSAGE];
      }
      else
      {
        shareLinkTask.Message = AppResources.SettingsPageShareLinkMessageDefault;
      }

      shareLinkTask.Show();
    }

    /// <summary>
    /// Handle the save button is clicked.
    /// </summary>
    void saveButton_Click(object sender, EventArgs args)
    {
      // retrieve the filename
      string[] urlString = imageUrl.ToString().Split(new char[] { '/' });
      var fileName = urlString[urlString.Length - 1];

      // start to download the picture and save it
      var webClient = new WebClient();
      webClient.OpenReadCompleted += (s, e) =>
      {
        SystemTray.ProgressIndicator.IsVisible = false;

        BitmapImage bitmap = new BitmapImage();
        bitmap.SetSource(e.Result);

        // save to media library in "saved pictures" album.
        using (var mediaLibrary = new MediaLibrary())
        {
          using (var stream = new MemoryStream())
          {
            WriteableBitmap wb = new WriteableBitmap(bitmap);
            wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 100);
            stream.Seek(0, SeekOrigin.Begin);
            var picture = mediaLibrary.SavePicture(fileName, stream);
            if (picture.Name.Contains(fileName))
            {
              MessageBox.Show(AppResources.SavePictureDoneText);
            }
          }
        }
      };
      webClient.OpenReadAsync(new Uri(imageUrl, UriKind.Absolute));
      SystemTray.ProgressIndicator.IsVisible = true;
    }

    /// <summary>
    /// Handle the lockscreen button is clicked.
    /// </summary>
    async void lockscreenButton_Click(object sender, EventArgs args)
    {
      if (!LockScreenManager.IsProvidedByCurrentApplication)
      {
        await LockScreenManager.RequestAccessAsync();
      }

      if (LockScreenManager.IsProvidedByCurrentApplication)
      {
        // retrieve the filename
        var fileName = string.Format("lockscreen-{0}.jpg", DateTime.Now.Ticks);

        // start to download the picture and save it
        var webClient = new WebClient();
        webClient.OpenReadCompleted += (s, e) =>
        {
          SystemTray.ProgressIndicator.IsVisible = false;

          BitmapImage bitmap = new BitmapImage();
          bitmap.SetSource(e.Result);

          using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
          {
            if (file.FileExists(fileName))
            {
              file.DeleteFile(fileName);
            }

            IsolatedStorageFileStream fileStream = file.CreateFile(fileName);
            WriteableBitmap wb = new WriteableBitmap(bitmap);
            System.Windows.Media.Imaging.Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 100);
            fileStream.Close();
          }

          LockScreen.SetImageUri(new Uri("ms-appdata:///Local/" + fileName, UriKind.Absolute));
          MessageBox.Show(AppResources.SetLockScreenDoneText);
        };
        webClient.OpenReadAsync(new Uri(imageUrl, UriKind.Absolute));
        SystemTray.ProgressIndicator.IsVisible = true;
      }
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
      imageUrl = item.ImageUrl;
      BitmapImage bitmap = new BitmapImage(new Uri(item.ImageUrl, UriKind.Absolute));
      ImageContainer.Source = bitmap;
      currentIndex = index;
    }
  }
}