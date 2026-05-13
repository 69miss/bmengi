using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSystem.AvaloniaUI;
public abstract class WindowCommBase<T> : Window where T : class
{
    public virtual T DataContextVal { get { return DataContext as T; } set { DataContext = value; } }
    private WindowNotificationManager? _notificationManager;
    public void ShowNotification(string content, bool noDuplicates = false)
    {
        if (_notificationManager == null)
        {
            _notificationManager = new WindowNotificationManager(TopLevel.GetTopLevel(this))
            {
                Position = NotificationPosition.BottomCenter,
                MaxItems = 3,
                Margin = new Thickness(0, 100, 0, 100)
            };
        }
        if (noDuplicates)
            _notificationManager.Close(content);
        _notificationManager.Show(

            content: content,
            type: NotificationType.Warning,
            expiration: TimeSpan.FromSeconds(3)
            //onClick: () => Console.WriteLine("通知被点击"),
            //onClose: () => Console.WriteLine("通知已关闭")
        );
    }
}
