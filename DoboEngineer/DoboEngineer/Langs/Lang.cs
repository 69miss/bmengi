using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DoboEngineer.code;
public class Lang : BaseNotifyPropertyChanged
{
    public static readonly Lang d = new Lang();
    public  Main Main { get; }
    Lang():base(null) {
        Main  = new Main(this);
    }
    public void ss()
    {

        using JsonDocument doc = JsonDocument.Parse("");
        JsonElement root = doc.RootElement;

        // 筛选出包含子模块的根节点（排除元数据节点）
        var moduleRootNodes = root.EnumerateObject()
            .Where(p =>
                p.Value.ValueKind == JsonValueKind.Object &&
                !new[] { "language", "description", "cultureName" }.Contains(p.Name)
            )
            .ToList();

        // 存储类节点信息：(类节点JSON元素, 完整命名空间, 类名)
        var classNodes = new List<(JsonElement Element, string Namespace, string ClassName)>();

        // 遍历所有模块根节点（例如JSON中的"Localization"节点）
        //如果有点分隔符认为是命名空间，否则就是类
        foreach (var rootNode in moduleRootNodes)
        {
            string rootNamespace = rootNode.Name; // 使用实际节点名作为根命名空间
            JsonElement rootNodeElement = rootNode.Value;

            // 遍历根节点下的一级模块（Main、DevelopModule等）
            foreach (var moduleNode in rootNodeElement.EnumerateObject())
            {
                string moduleName = moduleNode.Name; // 模块名
                JsonElement moduleElement = moduleNode.Value;

                // 遍历模块下的类节点（MainView、SettingView等）
                foreach (var classNode in moduleElement.EnumerateObject()) { }
            }
        }
        //写一个类型，里面是所有键值对，可以自动生成json,用于翻译
    }

    public string Title { get; private set; } = "标题{0}";
    public string 开始校准 { get; } = "开始校准";
}
public class Main : BaseNotifyPropertyChanged
{
    public Main(INotifyPropertyChanged baseNotifyProperty):base(baseNotifyProperty) {
    }
    public string Title { get; set; } = "Dobo Engineer";
}
public class BaseNotifyPropertyChanged : INotifyPropertyChangedExt2
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected BaseNotifyPropertyChanged(INotifyPropertyChanged baseNotifyProperty)
    {
        if (baseNotifyProperty == null)
            return;
        baseNotifyProperty.PropertyChanged += (p, p1) => { if (string.IsNullOrEmpty(p1.PropertyName)) OnAllChanged(); };
    }
    public void OnAllChanged() {
        OnPropertyChanged();
    }
    protected void OnPropertyChanged(string prop = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }


    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
}
