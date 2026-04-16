using DoboEngineer.code;
using IoTClient.Clients.PLC;
using IoTClient.Common.Enums;
using System;

namespace DoboEngineer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
       // public Lang L { get; set; } = Lang.d;
        public string Greeting { get; } = "Welcome to Avalonia!";
        public MainWindowViewModel() {
            test();
        }
        void test() {
            SiemensClient client = new SiemensClient(SiemensVersion.S7_200Smart, "127.0.0.1", 102);
            Console.WriteLine("s7:"+client);
            //client.Write("Q1.3", true);
            //client.Write("V2205", (short)11);
            //client.Write("V2209", 33);
            //client.Write("V2305", "orderCode");             //写入字符串

            ////3、读操作
            //var value1 = client.ReadBoolean("Q1.3").Value;
        }
    }
}
