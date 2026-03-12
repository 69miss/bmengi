using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DoboEngineer.Pump;
using System;
using System.Collections.ObjectModel;

namespace DoboEngineer.Pump
{
    public partial class ValListVM : ObservableObject
    {
        // 自动生成 public ObservableCollection<DataItemBase> DataItems { get; set; }
        [ObservableProperty]
        private ObservableCollection<IDataItemBase> _dataItems=new ObservableCollection<IDataItemBase>();

        public ValListVM()
        {
            //mock();
        }

        private void mock()
        {
            DataItems = new ObservableCollection<IDataItemBase>
            {
                new DataItemBase { Address = 0x0010, Name = "温度传感器(只读)", Value = 25.5, CanWrite = false },
                new DataItemBase { Address = 0x0011, Name = "报警阈值(可写)", Value = 40.0, CanWrite = true },
                new DataItemBase { Address = 0x0012, Name = "目标转速(可写)", Value = 1500, CanWrite = true },
                new DataItemBase { Address = 0x0013, Name = "运行状态(只读)", Value = 1, CanWrite = false }
            };

            // 模拟设备数据主动上报
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (sender, args) => SimulateDataChange();
            timer.Start();
        }

        private void SimulateDataChange()
        {
            var rand = new Random();
            DataItems[0].Value = Math.Round(25.0 + rand.NextDouble() * 5, 2);
            DataItems[3].Value = rand.Next(0, 2);
        }
    }
}