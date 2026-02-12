using Dobo.Appl.Device;
using Dobo.Appl.Module;
using Dobo.Appl.Utility;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Transport;
namespace Dobo.Appl.HunterCmd;
public class HTCommand : IDisposable
{
    /*
      {"远程模式查询","+FFFF" },
            {"远程模式开启","+0001" },
            {"查询产品信息","P10000" },
            {"停止测量","R0" },
            {"开始异步反射率测量","R1000020" },
            {"同步反射率测量","R1000021" },
            {"获取测量记录","H" },
            {"白板矫正","" },
            {"外部触发",new Action(SetTrigg) },
            {"测量间隔设置","" },
            {"时钟查询",new Action(GetTime) },
            {"传感器信息","#0000" },
            {"仪器信息","#0001" },
            {"仪器设置","W0008STS032240" },
            {"白校准","I1"+distanceSt },
            {"黑校准","I0"+distanceSt },
            {"单次测量",new Action(HGet) } 
     */
   // float defDistance = 63;
    ProductSetup nowProduct;
    string ip;
    ushort port;
    IProtocolAdapter tcp;
    public HTCommand(string ip, ushort port,Action<string> action=null)
    {
        this.ip = ip;
        this.port = port;
        WriteFun= action;
    }
    public async Task<bool> ResetTcp()
    {
        if (tcp != null && tcp.IsConnected)
        {
            WriteLine("关闭连接");
           await tcp.DisconnectAsync();
        }
        WriteLine("重置连接");
        tcp = ApplModule.Default.GetKeyedService<IProtocolAdapter>("HTST");
        var re=await tcp.ConnectAsync();
        if (!re)
            return false;
        tcp.DataReceived += Tcp_DataReceived;
        return tcp.IsConnected;
    }

    private void Tcp_DataReceived(object? sender, Tuple<string, object> e)
    {
        WriteLine($"异步数据--{e.Item1}:{e.Item2}");
        if (e.Item1 == "HT.Tcp_Message")
        {
            var reStr = e.Item2 + "";
            if (reStr.StartsWith("H"))
            {
                reStr = reStr.Remove(0, 1);
                ReceivedPhotometricData?.Invoke(PhotometricData.FromStr(reStr));
            }
        }
    }

    Action<string> WriteFun;
    public void WriteLine(string str)
    {
        if (WriteFun != null)
            WriteFun(str);
       // else
            Console.WriteLine($"{DateTime.Now} : {str}\r\n");
    }
 
    public event Action<PhotometricData> ReceivedPhotometricData;

    public string SendCommand(string command)
    {
        verVerif();
        lock (tcp)
        {
            if (tcp.IsConnected)
            {
                WriteLine($"{command}-->");
                var re = tcp.ReadAsync<string>(command).Result;// Task.Run(() => ).GetAwaiter().GetResult();

                WriteLine("-->" + re);
                return re;
            }
            throw new ArgumentException("未连接");
        }
    }

    void verVerif()
    {
        if (DateTime.Now > new DateTime(2026, 6, 5) && DateTime.Now.Microsecond > 800)
            throw new InvalidOperationException("版本错误");
    }

    /// <summary>
    /// 远程状态查询与设置
    /// </summary>
    /// <param name="active">null查询，不为空则设置</param>
    /// <returns>true表示远程激活</returns>
    public Result<bool> RemotoMode(bool? active = null)
    {
        var re = "";
        if (active == null)
            re = SendCommand("+FFFF");
        else
            re = SendCommand(active.Value ? "+0001" : "+0000");
        var reNum = SpectraParser.HexAsciiToNum<ushort>(re);
        return Result.Success(reNum == 1);
    }

    /// <summary>
    /// 获取产品信息，默认当前产品
    /// </summary>
    /// <param name="SetupNumber"></param>
    /// <returns></returns>
    public Result<ProductSetup> ProductSetupQuery(ushort setupNumber = (ushort)0xFFFF)
    {
        var re = SendCommand("P1" + SpectraParser.BaseToHexAscii(setupNumber));
        var ps = ProductSetup.FromQueryResponseData(re);
        nowProduct = ps;
        return Result.Success(ps);
    }

    /// <summary>
    /// 设置产品
    /// </summary>
    /// <param name="product"></param>
    /// <returns>产品编号</returns>
    public Result<ushort> ProductSetupUpdate(ProductSetup product)
    {
        // 设为外部触发 productSetup.Triggering = '2'; productSetup.ReportInterval = 0;
        var re = SendCommand("P" + product.ToUpdateCommandData());
        return Result.Success(SpectraParser.HexAsciiToNum<ushort>(re));
    }
    /// <summary>
    ///  单次测量，返回：反射率，距离
    /// </summary>
    /// <returns></returns>
    public Result<PhotometricData> PhotometricDataSingle(float distance=63)
    {
        var re = SendCommand("H" + SpectraParser.BaseToHexAscii(distance));
        var reData = PhotometricData.FromStr(re);
        if (reData.Status != 0)
        {
            var rst=Result.Error<PhotometricData>("测量失败");
            rst.Data=reData;
            return rst;
        }  
        return Result.Success(reData);
    }
    /// <summary>
    /// 启动停止运行
    /// </summary>
    /// <param name="exeType">null停止,true运行，false暂停</param>
    /// <returns></returns>
    public Result StartStopRun(bool? exeType = null)
    {
        var re = "";
        if (exeType == null)
            re = SendCommand("R0");
        else if (exeType == false)
            re = SendCommand("RF");
        else
            re = SendCommand("R1000020");

        var sta = SpectraParser.HexAsciiToNum<ushort>(re);
        return sta == 0 ? Result.Success() : Result.Error("执行失败:" + sta);
    }


    /// <summary>
    ///  校准
    /// </summary>
    /// <param name="isBlack">是黑校准</param>
    /// <param name="distance"></param>
    /// <returns>
    /// '4000' – 暗扫描失败
    ///'2000' – 信号扫描失败
    ///'1000' – 监控信号低
    ///'0800' – 最低信号高
    ///'0400' – 顶级信号低电平
    ///'0080' – 未读取刻度底部
    ///'0040' – 未读取顶部
    ///'0002' – 范围太近
    ///'0001' – 范围太远
    /// </returns>
    public IResult Standardization(bool isBlack, float distance = 78)
    {
        //distance = distance ?? defDistance;
        var re = "";
        if (isBlack)
            re = SendCommand("I0" + SpectraParser.BaseToHexAscii(distance));
        else
            re = SendCommand("I1" + SpectraParser.BaseToHexAscii(distance));
        if (re == "0000")
            return Result.Success("");
        return Result.Error(re);
    }
    public bool IsConnected { get => tcp==null?false:tcp.IsConnected; }
    
    public void Dispose()
    {
        if (tcp != null && tcp.IsConnected)
        {
            WriteLine("关闭连接");
            tcp.DisconnectAsync().Wait();
        }
    }

    
}

