using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.SPC100;
using Dobo.Appl.Utility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoboEngineer.ViewModels
{
    public partial class SpcUtilityVm: ViewModelBase,IDisposable
    {
        [ObservableProperty] 
        public partial string ShowTxt { get; set; } = "";
        [ObservableProperty]
        public partial bool HTSendBtnEnable { get; set; } = true;
        [ObservableProperty]
        public partial string HTCmdParam { get; set; } = "98";
        [ObservableProperty]
        public partial string SpcCmdParam { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<KeyValuePair<string,object>> HTCmdList { get; set; }
        [ObservableProperty] 
        public partial ObservableCollection<KeyValuePair<string, object>> SpcCmdList { get; set; }
        [ObservableProperty]
        public partial KeyValuePair<string, object>? NowHTCmd { get; set; }    
        [ObservableProperty]
        public partial KeyValuePair<string, object>? NowSpcCmd { get; set; }
        [ObservableProperty]
        public partial string RepeatBtnTxt { get; set; } = "重复执行";  
        CommandSet commandSet;
        SPCTcpCommand spcCmd;
        XYZCalc calc = Program.Default.GetService<XYZCalc>();
        public SpcUtilityVm() {
            CmdListInit();
        }

        public void Dispose()
        {
            commandSet?.Dispose();
            spcCmd?.Dispose();
        }
        string DistanceSt()
        {
            if (string.IsNullOrWhiteSpace(HTCmdParam))
            {
            }
            else if (float.TryParse(HTCmdParam, out var dist))
            {
                return SpectraParser.BaseToHexAscii(dist);
            }
            else
                Console.WriteLine("距离错误使用默认距离63");
            return "427C0000";//63mm
        }
        void CmdListInit() {
            var dic = new Dictionary<string, Object>()
            {
                {"光机连接：55:10001",new Action(async ()=>await HTConnectAsync() )},
                {"远程模式查询","+FFFF" },
                {"远程模式开启","+0001" },
                {"查询产品信息","P10000" },
                {"停止测量","R0" },
                {"开始异步反射率测量","R1000020" },
                {"同步反射率测量","R1000021" },
                {"获取测量记录","H" },
                //{"外部触发",new Action(SetTrigg) },
                //{"测量间隔设置","" },
                //{"时钟查询",new Action(GetTime) },
                {"传感器信息","#0000" },
                {"仪器信息","#0001" },
                {"仪器设置","W0008STS032240" },
                {"白校准()",new Action(()=> commandSet.SendCommand( "I1"+DistanceSt())) },
                {"黑校准()",new Action(()=> commandSet.SendCommand( "I0"+DistanceSt()))},
                {"单次测量()",new Action(()=>SingleGet()) },
                {"温度获取",new Action(()=>OptTemper()) },
                {"直接发送()",new Action(()=>commandSet.SendCommand( HTCmdParam)) },
            };
             HTCmdList=new ObservableCollection<KeyValuePair<string, object>>(dic.Select(p => p).ToArray());
            var dic2 = new Dictionary<string, Object>()
            {
                {"连接:7:50023",new Action<string>(SpcConnect) },
                {"状态信息读取",new Action<string>(p=>{var re=spcCmd?.ReadStatus();TxtWriteLine(re); }) },
                {"测量触发",IOFunctionCode.TriggerDetection },
                {"镜头盖打开",IOFunctionCode.OpenLensCover },
                {"镜头盖黑板",IOFunctionCode.CloseLensCover },
                {"镜头盖白板位",IOFunctionCode.MoveToWhitePosition },
                //{"v4",IOFunctionCode.v4 },
                //{"-镜头黑校准位","R1000020" },
                //{"-镜头绿校准位","R1000021" },
                {"光机电源开",IOFunctionCode.PowerOnLightMachine },
                {"光机电源关",IOFunctionCode.PowerOffLightMachine},
            };
            SpcCmdList=new ObservableCollection<KeyValuePair<string, object>>( dic2.Select(p => p).ToArray());
        }
        void TxtWriteLine(object txt) {
            if (ShowTxt.Length > 8000)
            {
                ShowTxt = ShowTxt.Substring(ShowTxt.Length-7000,7000);
            }
            ShowTxt +=$"{DateTime.Now}: {txt}\r\n";
        }
        
         
        [RelayCommand]
        public void RepeatHTCmdSend()
        {
            if (RepeatBtnTxt == "重复执行")
            {
                RepeatBtnTxt = "停止重复执行";
                RepeatCmd();
            }
            else
            {
                RepeatBtnTxt = "重复执行";
                return;
            }
            
        }

        private async Task RepeatCmd()
        {
            try
            {
                HTSendBtnEnable = false;
                while (RepeatBtnTxt != "重复执行")
                {
                    HTCmdSend();
                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                TxtWriteLine(ex.Message);

            }
            finally
            {
                HTSendBtnEnable = true;
            }
        }

        [RelayCommand]
        public void HTCmdSend()
        {
            if (NowHTCmd == null)
            { //TxtWriteLine(cbCmd.Text);
            }
            else
            {
                try
                {
                    var val = NowHTCmd.Value.Value;
                    if (val is string cmdStr)
                        commandSet.SendCommand(cmdStr);
                    else if (val is Action cmdFun)
                        cmdFun();
                }
                catch (Exception ex)
                {
                    TxtWriteLine(ex.Message);
                }
            }

        }
        [RelayCommand]
        public void SpcCmdSend()
        {
            if (NowSpcCmd == null)
            { //TxtWriteLine(cbCmd.Text);
            }
            else
            {
                try
                {

                    var val = NowSpcCmd.Value.Value;
                    if (val is IOFunctionCode cmd)
                        spcCmd.ExecuteIOCommand(cmd);
                    else if (val is Action<string> cmdFun)
                        cmdFun(SpcCmdParam);
                    else if (val is Action cmdFun2)
                        cmdFun2();
                }
                catch (Exception ex)
                {
                    TxtWriteLine(ex.Message);
                }
            }
        }
        
           
       public void SpcConnect(string p)
        {
            spcCmd?.Dispose();
            spcCmd = new SPCTcpCommand();
            spcCmd.Connect();
            TxtWriteLine("spc连接成功");
        } 
       
        public async Task HTConnectAsync() {
            try
            {
                if (commandSet != null && commandSet.IsConnected)
                {
                    commandSet.StartStopRun();
                    commandSet.Dispose();
                    commandSet = null;
                    return;
                }
                commandSet = new CommandSet("192.168.0.55", 10001, new Action<string>(TxtWriteLine));
                await commandSet.ResetTcp();
                commandSet.RemotoMode(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
            }
        }

        void SingleGet()
        {
            if (!float.TryParse(HTCmdParam, out var dist))
            {
                TxtWriteLine("距离错误");
                return;
            }
            var data = commandSet.PhotometricDataSingle(dist);
            if (!data.IsSuccess())
                return;
            var xyz = calc.CalcXyzByR(data.Data.Data.Select(p => (double)p).ToArray());
            var lab = calc.XyzToLab(xyz[0], xyz[1], xyz[2]);
            var arrStr = string.Join(",", data.Data.Data);
            TxtWriteLine($"--------{dist}-----------------------------");
            TxtWriteLine($"反射率: {arrStr}");
            TxtWriteLine($"xyz:{xyz[0]},{xyz[1]},{xyz[2]}");
            TxtWriteLine($"lab:{lab[0]},{lab[1]},{lab[2]}");
            IConvertible[] list =[DateTime.Now+"", dist, "反射率",.. data.Data.Data,"xyz",..xyz,"lab",..lab] ;

            CsvTool.ToCsv([list], filePath:"dataRecord.csv");
        }
        void OptTemper()
        {
            var count = SpectraParser.BaseToHexAscii((ushort)1);
            var str = commandSet.SendCommand($"@P1{count}A012");

            var pars = SpectraParser.Read(str);
            pars.readCmdChar(1);
            var reCount = pars.IntegerItem;
            var indexStr = pars.readCmdChar(4);
            var len = pars.UnsignedItem;
            var reVal = pars.get_SingleArray(2);
            TxtWriteLine($"count:{reCount},index:{indexStr},val:{reVal[0]}");
        }
    }
}
