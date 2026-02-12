using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoboEngineer;

public class LanguageService : INotifyPropertyChangedExt2
{
    // 实现索引器
   // public string this[string key] => _currentDict.ContainsKey(key) ? _currentDict[key] : key;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ChangeLanguage(string langCode)
    {
        // ... 加载 JSON 逻辑 ...
     //   _currentDict = LoadJson(langCode);
        // 通知界面所有属性都变了("Item[]" 是索引器的特定通知名)
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
}
