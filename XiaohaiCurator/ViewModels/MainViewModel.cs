using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using XiaohaiCurator.Resources;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;

namespace XiaohaiCurator.ViewModels
{
  public class MainViewModel : BaseViewModel
  {
    const string TOKEN = "YOUR_TOKEN_HERE";

    public MainViewModel()
    {
      this.DailyGirls = new ObservableCollection<GirlViewModel>();
      this.GirlsStream = new ObservableCollection<GirlViewModel>();
      this.GirlOfToday = new ObservableCollection<GirlViewModel>();
      this.IsStreamEnd = false;
    }

    public ObservableCollection<GirlViewModel> DailyGirls { get; private set; }
    public ObservableCollection<GirlViewModel> GirlsStream { get; private set; }
    public ObservableCollection<GirlViewModel> GirlOfToday { get; private set; }

    public bool IsDataLoaded
    {
      get;
      private set;
    }

    private bool _isLoading = false;
    public bool IsLoading
    {
      get
      {
        return _isLoading;
      }
      private set
      {
        _isLoading = value;
        NotifyPropertyChanged("IsLoading");
      }
    }

    public bool IsStreamEnd
    {
      get;
      private set;
    }

    public async void LoadGirlOfDay(string day)
    {
      this.IsLoading = true;

      this.GirlOfToday.Clear();

      var result = await DownloadStringAsync(
        new Uri(string.Format("http://curator.im/api/girl_of_the_day/{0}/?token={1}&format=json", day, TOKEN)));

      JArray images = JArray.Parse(result);
      int count = images.Count;
      
      for (int i = 0; i < count; ++i )
      {
        JObject image = (JObject)images[i];

        this.GirlOfToday.Add(new GirlViewModel() 
        { 
          ThumbnailUrl = new Uri((string)image["thumbnail"], UriKind.Absolute),
          ImageUrl = new Uri((string)image["image"], UriKind.Absolute)
        });
      }

      this.IsLoading = false;
    }

    /// <summary>
    /// Load girl stream (正妹流) by page
    /// </summary>
    /// <param name="page">page of girl stream</param>
    public async void LoadStream(int page)
    {
      this.IsLoading = true;
      var result = await DownloadStringAsync(new Uri(String.Format("http://curator.im/api/stream/?token={0}&page={1}&format=json", TOKEN, page)));

      JObject sResult = JObject.Parse(result);
      string next = (string)sResult["next"];

      if (next == null)
      {
        this.IsStreamEnd = true;
      }

      JArray stream = (JArray)sResult["results"];
      int count = stream.Count;

      for (int i = 0; i < count; ++i)
      {
        JObject g = (JObject)stream[i];
        this.GirlsStream.Add(new GirlViewModel()
        {
          Name = (string)g["name"],
          ThumbnailUrl = new Uri((string)g["thumbnail"]),
          ImageUrl = new Uri((string)g["image"], UriKind.Absolute),
          DateAt = (string)g["date"]
        });
      }
      this.IsLoading = false;
    }

    /// <summary>
    /// Load initial data.
    /// </summary>
    public async void LoadData()
    {
      this.IsLoading = true;

      try
      {
        var result = await DownloadStringAsync(new Uri(String.Format("http://curator.im/api/girl_of_the_day/?token={0}&format=json", TOKEN)));

        JObject obj = JObject.Parse(result);
        JArray results = (JArray)obj["results"];
        JObject daily = (JObject)results[0];
        int count = results.Count;

        for (int i = 0; i < count; ++i)
        {
          JObject item = (JObject)results[i];
          this.DailyGirls.Add(new GirlViewModel() 
          {
            Name = (string)item["name"],
            ThumbnailUrl = new Uri((string)item["thumbnail"]),
            ImageUrl = new Uri((string)item["image"], UriKind.Absolute),
            DateAt = (string)item["date"]
          });
        }

        // save the image for lockscreen change
        SaveImage((string)daily["image"], string.Format("Daily-{0}.jpg", (string)daily["date"]));

        ChangeLiveTiles((string)daily["name"], (string)daily["thumbnail"]);
        this.IsLoading = false;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error: " + ex.Message);
      }

      this.IsDataLoaded = true;
    }

    /// <summary>
    /// Update live tile information.
    /// </summary>
    /// <param name="backTitle">backside title</param>
    /// <param name="today">the date for tile image</param>
    private void ChangeLiveTiles(string backTitle, string today)
    {
      ShellTile mainTile = ShellTile.ActiveTiles.FirstOrDefault();
      if (null != mainTile)
      {
        FlipTileData tile = new FlipTileData() 
        {
          Title = AppResources.ApplicationTitle,
          BackTitle = backTitle,
          BackBackgroundImage = new Uri(today)
        };
        try
        {
          mainTile.Update(tile);
        }
        catch (Exception ex)
        {
          Debug.WriteLine("Error: " + ex.Message);
        }
      }
    }

    /// <summary>
    /// Save image to IsolatedStorage.
    /// </summary>
    /// <param name="imageUrl">The URL of the image.</param>
    /// <param name="fileName">Filename to save.</param>
    private void SaveImage(string imageUrl, string fileName)
    {
      var webClient = new WebClient();
      webClient.OpenReadCompleted += (s, e) => 
      {
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

      };
      webClient.OpenReadAsync(new Uri(imageUrl, UriKind.Absolute));
    }


    /// <summary>
    /// Asynchronous HTTP Get helper
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    private Task<string> DownloadStringAsync(Uri uri)
    {
      var client = new WebClient();
      var tcs = new TaskCompletionSource<string>();

      try
      {
        client.DownloadStringCompleted +=
          (s, e) =>
            {
              if (e.Error == null)
              {
                tcs.TrySetResult(e.Result);
              }
              else
              {
                tcs.TrySetException(e.Error);
              }
            };
        client.DownloadStringAsync(uri);
      }
      catch (Exception ex)
      {
        tcs.TrySetException(ex);
      }

      return tcs.Task;
    }
  }
}