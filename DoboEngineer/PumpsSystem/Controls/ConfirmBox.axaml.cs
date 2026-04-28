using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PumpsSystem;
    public class LabelValueVM : INotifyPropertyChanged
    {
        public string Label { get; set; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
        public string Label2 { get;set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
public partial class ConfirmBox : Window
{
    // 内部数据模型，用于绑定到动态生成的输入框


    private List<LabelValueVM> _inputModels;

    // 默认构造函数（必须保留给 XAML 预览器使用）
    public ConfirmBox()
    {
        InitializeComponent();
    }

    // 带参数的构造函数
    private ConfirmBox(string title, string promptText, IEnumerable<string> inputLabels=null) : this()
    {
        Title = title;
        PromptTextBlock.Text = promptText;

        // 根据传入的字符串列表，初始化输入框的数据源
        _inputModels = new List<LabelValueVM>();
        if (inputLabels != null)
        {
            foreach (var label in inputLabels)
            {
                var vm = new LabelValueVM { Label = label, Value = "" };
                if (vm.Label.StartsWith("*")) {
                    vm.Label2 = "*";
                }
                _inputModels.Add(vm);
            }
        }

        // 绑定到 ItemsControl
        InputsItemsControl.ItemsSource = _inputModels;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // 点击取消时，返回 null
        Close(null);
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        // 点击确认时，将用户的输入提取为字典并返回
        var result = _inputModels.ToDictionary(m => m.Label, m => m.Value);
        Close(result);
    }

    /// <summary>
    /// 弹出一个动态确认输入框
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="title">窗口标题</param>
    /// <param name="promptText">提示文本内容</param>
    /// <param name="inputLabels">需要生成的输入框的标签名称集合</param>
    /// <returns>返回一个字典：键为输入框标签，值为用户输入的内容。如果用户取消则返回 null。</returns>
    public static async Task<Dictionary<string, string>> ShowAsync(
        Window parent,
        string title,
        string promptText,
       params IEnumerable<string> inputLabels)
    {
        var dialog = new ConfirmBox(title, promptText, inputLabels);
        return await dialog.ShowDialog<Dictionary<string, string>>(parent);
    }
}