using System;

namespace XiaohaiCurator.ViewModels
{
  public class GirlViewModel : BaseViewModel
  {
    private string _id;
    public string Id
    {
      get
      {
        return _id;
      }
      set
      {
        if (value != _id)
        {
          _id = value;
          NotifyPropertyChanged("Id");
        }
      }
    }

    private string _name;
    /// <summary>
    /// The name of the daily girl.
    /// </summary>
    public string Name
    {
      get 
      {
        return _name;
      }
      set
      {
        if (value != _name)
        {
          _name = value;
          NotifyPropertyChanged("Name");
        }
      }
    }

    private string _thumbnailUrl;
    /// <summary>
    /// 
    /// </summary>
    public string ThumbnailUrl
    {
      get 
      { 
        return _thumbnailUrl; 
      }
      set
      {
        if (value != _thumbnailUrl)
        {
          _thumbnailUrl = value;
          NotifyPropertyChanged("ThumbnailUrl");
        }
      }
    }

    private string _imageUrl;
    public string ImageUrl
    {
      get
      {
        return _imageUrl;
      }
      set
      {
        if (value != _imageUrl)
        {
          _imageUrl = value;
          NotifyPropertyChanged("ImageUrl");
        }
      }
    }

    private string _dateAt;
    public string DateAt
    {
      get
      {
        return _dateAt;
      }
      set
      {
        if (value != _dateAt)
        {
          _dateAt = value;
          NotifyPropertyChanged("DateAt");
        }
      }
    }
  }
}
