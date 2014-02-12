using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Windows;
using Windows.Phone.System.UserProfile;
using Microsoft.Phone.Scheduler;

namespace HandOverTaskAgent
{
  public class ScheduledAgent : ScheduledTaskAgent
  {
    private DateTime today;
    private string fileName;
    private Uri dailyImageUri;

    /// <remarks>
    /// ScheduledAgent 建構函式，會初始化 UnhandledException 處理常式
    /// </remarks>
    static ScheduledAgent()
    {
      // 訂閱 Managed 例外狀況處理常式
      Deployment.Current.Dispatcher.BeginInvoke(delegate
      {
        Application.Current.UnhandledException += UnhandledException;
      });
    }

    /// 發生未處理的例外狀況時要執行的程式碼
    private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
    {
      if (Debugger.IsAttached)
      {
        // 發生未處理的例外狀況; 切換到偵錯工具
        Debugger.Break();
      }
    }

    /// <summary>
    /// 執行排程工作的代理程式
    /// </summary>
    /// <param name="task">
    /// 叫用的工作
    /// </param>
    /// <remarks>
    /// 這個方法的呼叫時機為叫用週期性或耗用大量資料的工作時
    /// </remarks>
    protected override void OnInvoke(ScheduledTask task)
    {
      today = DateTime.Today;
      fileName = string.Format("Daily-{0:0000}-{1:00}-{2:00}.jpg", today.Year, today.Month, today.Day);
      dailyImageUri = new Uri(string.Format("ms-appdata:///Local/{0}", fileName), UriKind.Absolute);

      ChangeLockscreenPicture();
      NotifyComplete();
    }

    private void ChangeLockscreenPicture()
    {
      if (LockScreenManager.IsProvidedByCurrentApplication)
      {
        using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
        {
          if (file.FileExists(fileName))
          {
            LockScreen.SetImageUri(dailyImageUri);
          }
        }
      }
    }
  }
}