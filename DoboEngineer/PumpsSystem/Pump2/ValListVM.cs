using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PumpsSystem.Pump;
using System;
using System.Collections.Generic;
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
                new DataItemProp(25.5) { Address = "1", Name = "温度传感器(只读)", Value = 25.5, CanWrite = false },
                new DataItemProp(63340) { Address = "2", Name = "报警阈值(可写)", Value = 63340, CanWrite = true },
                new DataItemProp(1500) { Address = "3", Name = "目标转速(可写)", Value = 1500, CanWrite = true },
                new DataItemProp(true) { Address = "4", Name = "运行状态(只读)", Value = true, CanWrite = false }
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

        public static IConvertible? Convert2(object val, TypeCode typeCode)
        {
            return Convert.ChangeType(val, typeCode) as IConvertible;
            if (val is DBNull)
                val = null;
            if (val == null)
            {
                return typeCode switch
                {
                    TypeCode.Empty => null,
                    TypeCode.Object => null,
                    TypeCode.DBNull => DBNull.Value,
                    TypeCode.Boolean => default(bool),
                    TypeCode.Char => default(char),
                    TypeCode.SByte => default(sbyte),
                    TypeCode.Byte => default(byte),
                    TypeCode.Int16 => default(short),
                    TypeCode.UInt16 => default(ushort),
                    TypeCode.Int32 => default(int),
                    TypeCode.UInt32 => default(uint),
                    TypeCode.Int64 => default(long),
                    TypeCode.UInt64 => default(ulong),
                    TypeCode.Single => default(float),
                    TypeCode.Double => default(double),
                    TypeCode.Decimal => default(decimal),
                    TypeCode.DateTime => default(DateTime),
                    TypeCode.String => null,
                    _ => throw new ArgumentException($"不支持的TypeCode: {typeCode}", nameof(typeCode))
                };
            }
            try
            {

                return typeCode switch
                {
                    TypeCode.Boolean => System.Convert.ToBoolean(val),
                    TypeCode.Char => System.Convert.ToChar(val),
                    TypeCode.SByte => System.Convert.ToSByte(val),
                    TypeCode.Byte => System.Convert.ToByte(val),
                    TypeCode.Int16 => System.Convert.ToInt16(val),
                    TypeCode.UInt16 => System.Convert.ToUInt16(val),
                    TypeCode.Int32 => System.Convert.ToInt32(val),
                    TypeCode.UInt32 => System.Convert.ToUInt32(val),
                    TypeCode.Int64 => System.Convert.ToInt64(val),
                    TypeCode.UInt64 => System.Convert.ToUInt64(val),
                    TypeCode.Single => System.Convert.ToSingle(val),
                    TypeCode.Double => System.Convert.ToDouble(val),
                    TypeCode.Decimal => System.Convert.ToDecimal(val),
                    TypeCode.DateTime => System.Convert.ToDateTime(val),
                    TypeCode.String => System.Convert.ToString(val),
                    TypeCode.DBNull => DBNull.Value,
                    TypeCode.Empty => null,
                    TypeCode.Object => val as IConvertible ?? throw new InvalidCastException($"Type {val.GetType().Name} 未实现IConvertible，不支持TypeCode.Object转换"),
                    _ => throw new ArgumentException($"无效的TypeCode: {typeCode}", nameof(typeCode))
                };
            }
            catch (InvalidCastException ex)
            {
                throw;
            }

        }
       
        [RelayCommand]
       public async Task Send(IList<object> paramArr)
        {

   
            var param = (dataItem: paramArr[0] as IDataItemProp, txt: paramArr[1] as string);
            
            await PumpCmd.WriteValue(param.dataItem.Address, Convert2(param.txt,param.dataItem.TypeCode));

        }
    }
}