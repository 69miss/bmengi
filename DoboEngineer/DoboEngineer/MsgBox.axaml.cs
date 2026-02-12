using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dobo.Appl.Utility;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DoboEngineer;

public partial class MsgBox : Window, INotifyPropertyChangedExt2
{
    string contentTxt;

    public string ContentTxt { get => contentTxt; set => NotifyThis.SetField(ref contentTxt, value); }
    bool showCancel=true;
      INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public bool ShowCancel { get => showCancel; set => NotifyThis.SetField(ref showCancel, value); }

    public MsgBox()
    {
        InitializeComponent();
        CanMinimize = false;

    }
    public MsgBox(Window parent, string message, string title) :base()
    {
        
    }
    // 1. 静态辅助方法：模拟 MessageBox.Show
    // TResult 是我们定义的枚举
    public static Task<int> Show(Window parent, string title, string message,bool showCancel = true)
    {
        var msgBox = new MsgBox();
        msgBox.Title = title;
        msgBox.showCancel = showCancel;
        // 找到控件并赋值
        var textBlock = msgBox.FindControl<TextBlock>("MessageText");
        if (textBlock != null) textBlock.Text = message;

        // 显示模态窗口，并等待结果
        return msgBox.ShowDialog<int>(parent);
    }

    // 2. 确认按钮逻辑
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        // 关闭窗口，并返回 Ok 状态
        Close(1);
    }

    // 3. 取消按钮逻辑
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // 关闭窗口，并返回 Cancel 状态
        Close(0);
    }
    private static Window? GetActiveWindow()
    {
        // 判断当前应用生命周期是否是桌面模式
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 优先尝试获取当前处于“激活/聚焦”状态的窗口 (处理多窗口情况)
            var activeWindow = desktop.Windows.FirstOrDefault(w => w.IsActive);

            // 2. 如果没有激活窗口，这就回退使用主窗口 (MainWindow)
            return activeWindow ?? desktop.MainWindow;
        }

        return null;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
}