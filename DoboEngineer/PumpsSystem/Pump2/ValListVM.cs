using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PumpsSystem.Pump;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2
{
    public partial class ValListVM : ObservableObject
    {
        // 自动生成 public ObservableCollection<DataItemBase> DataItems { get; set; }
        [ObservableProperty]
        private ObservableCollection<IDataItemProp> _dataItems=new ObservableCollection<IDataItemProp>();
        internal PumpCmd PumpCmd { get; set; }
        public ValListVM()
        {
            if (Design.IsDesignMode)
                mock();
        }

        private void mock()
        {
            DataItems = new ObservableCollection<IDataItemProp>
            {
                new DataItemProp { Address = "1", Name = "温度传感器(只读)", Value = 25.5, CanWrite = false },
                new DataItemProp { Address = "2", Name = "报警阈值(可写)", Value = 63340, CanWrite = true },
                new DataItemProp { Address = "3", Name = "目标转速(可写)", Value = 1500, CanWrite = true },
                new DataItemProp { Address = "4", Name = "运行状态(只读)", Value = 1, CanWrite = false }
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
        [RelayCommand]
        async Task Send(object[] paramArr)
        {

            var param = (dataItem: paramArr[0] as IDataItemProp, txt: paramArr[1] as string);
            await PumpCmd.WriteValue(param.dataItem.Address, param.txt);

        }
    }
}