using Dobo.Appl.Entity;
using Dobo.Appl.Service;
using Dobo.Appl.SPC100;
using Dobo.Appl.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dobo.Appl.HunterCmd
{
   public class DeviceServiceCfg
    {
        /// <summary>
        /// 校准时距离
        /// </summary>
        public float StandardizationDistance { get; set; }= 63;
        /// <summary>
        /// 工作时距离
        /// </summary>
        public float WorkDistance { get; set; } = 98;
        /// <summary>
        /// 断纸后运行延时,毫秒
        /// </summary>
        public int DelayPaperBreak { get; set; }= 10 * 1000;
        /// <summary>
        /// 基准白反射率
        /// </summary>
        public float[] StandardWRefBy30 { get; set; }
        /// <summary>
        /// 光机测量间隔
        /// </summary>
        public int OpticalWorkInterval { get; set; } = 30;

        /// <summary>
        /// 开始时进行校准
        /// </summary>
        public bool BeginRunStandardization { get;set; }= false;
        /// <summary>
        /// 运行中自动校准间隔
        /// </summary>
        public int AutoStandardizationInterval { get; set; } =0;

        /// <summary>
        /// 断纸后校准
        /// </summary>
        public int PaperBreakStandardization { get; set; } = 0;
        /// <summary>
        /// 外部校准后,内白值
        /// </summary>
        public float[] WhiteByOutStandard { get; set; }
    }
    public class DeviceService:IWorkService
    {
        private  PeriodicTimer timer;
        private  CancellationTokenSource cts;
        SemaphoreSlim semaphore = new SemaphoreSlim(1);
        CommandSet commandSet;
        SPCTcpCommand spcCmd;
        OpticalDataSvc opticalDataSvc;
        Microsoft.Extensions.Logging.ILogger logger;
        
        readonly ValueTuple<string, ushort> OpticalAddr;
        readonly ValueTuple<string, ushort> SpcAddr;
       readonly static SnowflakeIdWorker snowflakeIdWorker=new SnowflakeIdWorker(2,0);
        //readonly float standardizationDistance = 63;
        //readonly float workDistance = 98;
        //readonly int delayPaperBreak = 10 * 1000;
        //readonly float[] standardWRefBy30;
        //private readonly int seconds;
        DeviceServiceCfg cfg;
        /// <summary>
        /// 间隔时间
        /// </summary>
        /// <param name="seconds"></param>
        public DeviceService(Microsoft.Extensions.Logging.ILogger<DeviceService> log, string ip = "192.168.0.55", ushort port = 10001, long productId = 0, DeviceServiceCfg serviceCfg = null)
        {
            OpticalAddr = (ip, port);
            SpcAddr = ("192.168.0.7", 50023);
            logger = log;

            ProductId = productId;
            //timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
            opticalDataSvc = new OpticalDataSvc();
            cfg = serviceCfg;
            Connect();
            WhiteByOutStandard =cfg.WhiteByOutStandard ;//dataDictSvc.GetByJson<float[]>(nameof(WhiteByOutStandard));
        }
        public Action<Result<OpticalData>> PhotometricDataReturnHandle { get; set; }

        public void Dispose()
        {
            timer?.Dispose();
            commandSet?.Dispose();
            spcCmd?.Dispose();
        }
        void Connect()
        {
            var i = 0;
            while (i++ < 3)
            {
                try
                {
                    spcCmd?.Dispose();
                    spcCmd = new SPCTcpCommand();
                    spcCmd.Connect();
                    //
                    commandSet?.Dispose();
                    commandSet = new CommandSet(OpticalAddr.Item1, OpticalAddr.Item2, p => logger.LogInformation(p));

                    if (commandSet.ResetTcp().Result)
                    {
                        commandSet.RemotoMode(true);
                        break;
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("连接失败：" + e);
                }
                if (i < 3)
                    logger.LogWarning("commandSet连接失败,重新连接:" + i);
                else
                    throw new ArgumentException("commandSet连接失败");
            }
        }
         async Task StartBatchAsync(int delay=2000)
        {
            if (timer!=null||(cts!=null&&!cts.IsCancellationRequested))
            {
                logger.LogWarning("StartBatchAsync重复启动，已取消本次");
                return;
            }
            logger.LogInformation($"Starting timer...间隔：{cfg.OpticalWorkInterval} 秒;设定距离:白{cfg.StandardizationDistance},试样{cfg.WorkDistance};");
            timer = new PeriodicTimer(TimeSpan.FromSeconds(cfg.OpticalWorkInterval));
            cts = new CancellationTokenSource();
            await Task.Delay(delay);
            if (!isRun)
            {
                logger.LogInformation("检测已停止！StartBatchAsync不再执行");
                return;
            }
            try
            {
                do
                {
                    try
                    {
                        logger.LogInformation($"{DateTime.Now}：定时调用");
                        var spcStatus = spcCmd.ReadStatus();
                        //白测量
                        Console.WriteLine("白测量");
                        spcCmd.ExecuteIOCommand(IOFunctionCode.MoveToWhitePosition);
                        await Task.Delay(2000);
                        var wRef = PhotometricCheck(OpticalData.R400_700_10_W, cfg.StandardizationDistance, "1白", spcStatus.Temperature);
                        await Task.Delay(1000);
                        spcCmd.ExecuteIOCommand(IOFunctionCode.CloseLensCover);
                        await Task.Delay(2000);
                        //黑测试
                        //PhotometricCheck(OpticalData.R400_700_10_B, cfg.StandardizationDistance, "1_2黑", spcStatus.Temperature);
                        //await Task.Delay(1000);
                        //
                        spcCmd.ExecuteIOCommand(IOFunctionCode.OpenLensCover);
                        await Task.Delay(2000);
                        if (nowSpcStateInfo == null || !nowSpcStateInfo.PaperBreakSignal)
                        {
                            var rRef = PhotometricCheck(OpticalData.R400_700_10, cfg.WorkDistance, "2样品", spcStatus.Temperature);
                            var newRef = RefTransform(wRef.Data, rRef.Data, spcStatus.Temperature);

                        }
                        await Task.Delay(1000);
                        spcCmd.ExecuteIOCommand(IOFunctionCode.CloseLensCover);
                        await AutoStandardization();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "测量执行异常");
                    }
                    finally
                    {

                    }
                } while (timer != null && await timer.WaitForNextTickAsync(cts.Token));
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Timer was canceled.");
            }
            logger.LogInformation("Timer stopped.");
        }
        public float[] RefTransform(PhotometricData wData, PhotometricData rData, float temperature)
        {
            if (cfg.StandardWRefBy30 == null || cfg.StandardWRefBy30.Length < 31||rData==null)
                return null;
            float[] wRef = wData.Data;
            float[] rRef = rData.Data;
            var newRef = new float[cfg.StandardWRefBy30.Length];
            if (WhiteByOutStandard == null || WhiteByOutStandard.Length < 31)
                WhiteByOutStandard = cfg.StandardWRefBy30;
            for (int i = 0; i < cfg.StandardWRefBy30.Length; i++)
            {
                var cvar = cfg.StandardWRefBy30[i] / WhiteByOutStandard[i];
                newRef[i] = rRef[i]/ wRef[i] * cfg.StandardWRefBy30[i]*cvar;
            }
            //
            var batchNum = snowflakeIdWorker.NextId();
            var entity = new OpticalData() { BatchNum = batchNum, DataType = OpticalData.FromStandardWRefBy30, DataArrObj = newRef.Select(p => (double)p).ToArray(), CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Remark = "反射率修正", Temperature = temperature };
            var id = opticalDataSvc.Add(entity);
            PhotometricDataReturnHandle?.Invoke( Result.Success(entity));
            //
            return newRef;
        }
        public DateTime? lastStandardizationTime;
        public async Task AutoStandardization()
        {
            //校准判断
            if (cfg.AutoStandardizationInterval < 1)
                return;
            if (lastStandardizationTime != null
                && (DateTime.Now- lastStandardizationTime).Value.TotalSeconds < cfg.AutoStandardizationInterval)
            {
                return;
            }
            await StandardSetAsync(null);
            //lastStandardizationTime = DateTime.Now;
        }

        //Action<object> DataPushFun;
        /// <summary>
        /// 开始定时测量：打开镜头盖，测量，保存
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            //整体开始，1 spc循环状态，2光机测量
            isRun = true;
            if (cfg.BeginRunStandardization) {
              await  StandardSetAsync(null);
            }
            SpcStatePullLoop();

        }
        Result<PhotometricData> PhotometricCheck(string type, float distance, string remark=null,float temperature=0)
        {
            try
            {
                var beginTime = DateTimeOffset.Now;
                var data = commandSet.PhotometricDataSingle(distance);
                //保存数据库//推送
                if (!data.IsSuccess())
                {
                    logger.LogWarning("测量数据PhotometricDataSingle返回错误信息：" + data.Message);
                }
                else
                {
                    if (nowSpcStateInfo != null &&nowSpcStateInfo.PaperBreakSignal)
                        return Result.Error<PhotometricData>("断纸中断测量"); ;
                        var batchNum = snowflakeIdWorker.NextId();
                    var entity = new OpticalData() { BatchNum = batchNum, DataType = type, DataArrObj = data.Data.Data.Select(p => (double)p).ToArray(), CreateTime = beginTime, RecordTime = DateTimeOffset.Now, Remark = remark,Temperature= temperature };
                    var id = opticalDataSvc.Add(entity);
                    data.Message = batchNum + "";
                    //if (type == OpticalData.R400_700_10)
                        PhotometricDataReturnHandle?.Invoke( Result.Success(entity));
                }
                return data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "单次测量命令异常");
                return Result.Error<PhotometricData>("单次测量命令异常");
            }
        }
        /// <summary>
        /// 开始定时测量,lock同步
        /// </summary>
        /// <returns></returns>
        public async Task StartSingleAsync() {
            Console.WriteLine("Starting timer...");
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    if (await semaphore.WaitAsync(0))
                    {
                        try
                        {
                            Console.WriteLine($"Tick at {DateTime.Now}");
                            //打开镜头

                            //测量
                            var beginTime = DateTimeOffset.Now;
                            var data= commandSet.PhotometricDataSingle();
                            //保存数据库//推送
                            if (!data.IsSuccess())
                            {

                            }
                            else
                            {
                                var entity = new OpticalData() {ProductId=ProductId, DataArrObj = data.Data.Data.Select(p => (double)p).ToArray(), CreateTime = beginTime, RecordTime = DateTimeOffset.Now };
                                opticalDataSvc.Add(entity);
                                PhotometricDataReturnHandle?.Invoke(Result.Success(entity));
                            } 
                        }
                        catch (Exception ex)
                        {

                        }
                        finally
                        {
                            semaphore.Release();
                        }

                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Timer was canceled.");
            }
            finally
            {
                 timer.Dispose();
            }

            Console.WriteLine("Timer stopped.");
        }




        public Task StopAsync()
        {
            isRun = false;
            OpStop();
            return Task.CompletedTask;
        }
         void OpStop() {
            cts?.Cancel();
            timer?.Dispose();
            timer = null;
            //cts?.Dispose();
            //cts = null;
        }
        public Action<Result<Tuple<SpcStateInfo, OpticalData[]>>> StateHandle {  get; set; }

        public long ProductId { get; }
        public Action<Tuple<int, string, object>> MsgInfoHandle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        SpcStateInfo nowSpcStateInfo;
        bool isRun;

        public void SpcStatePullLoop2()
        {

            Task.Run(async () =>
            {
                do
                {
                    Console.WriteLine("SpcStatePullLoop begin !");
                    var newState = spcCmd.ReadStatus();
                    var opticalDataList = new List<OpticalData>();
                    var nowSpcState = nowSpcStateInfo;
                    //断纸停并记录，恢复直接开始测量
                    if (newState.PaperBreakSignal != nowSpcState.PaperBreakSignal && newState.PaperBreakSignal)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignal, Remark = "断纸",Temperature=newState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    else if (newState.PaperBreakSignal != nowSpcState.PaperBreakSignal && newState.PaperBreakSignal == false)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignalEnd, Remark = "断纸结束", Temperature = newState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    if (nowSpcState.LowerMachineSignal != newState.LowerMachineSignal && nowSpcState.LowerMachineSignal)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.LowerMachineSignal, Remark = "下机", Temperature = newState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    opticalDataSvc.Add(opticalDataList.ToArray());
                    nowSpcState = newState;
                    StateHandle?.Invoke(Result.Success(Tuple.Create(nowSpcState, opticalDataList.ToArray())));
                    Task.Delay(1500);
                } while (isRun);
                Console.WriteLine("SpcStatePullLoop stop !");
            });
        }
        void SpcStatePullLoop()
        {
           
            Task.Run(async () =>
            {
                do
                {
                    Console.WriteLine("SpcStatePullLoop begin !");
                    var newState = spcCmd.ReadStatus();
                    var opticalDataList = new List<OpticalData>();
                    //第一次，判断是否启动
                    if (nowSpcStateInfo==null)
                    {
                        if(!newState.PaperBreakSignal)
                            StartBatchAsync();
                        nowSpcStateInfo = newState;
                        continue;
                    }
                    var nowSpcState = nowSpcStateInfo;
                    //断纸停并记录，恢复直接开始测量
                    if (newState.PaperBreakSignal != nowSpcState.PaperBreakSignal && newState.PaperBreakSignal)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now,Temperature=newState.Temperature,  DataType = OpticalData.PaperBreakSignal, Remark = "断纸" };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                        OpStop();
                    }
                    else if (  newState.PaperBreakSignal != nowSpcState.PaperBreakSignal && newState.PaperBreakSignal == false)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Temperature = newState.Temperature, DataType = OpticalData.PaperBreakSignalEnd, Remark = "断纸结束" };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                        if (cfg.PaperBreakStandardization == 1)
                        {
                            await StandardSetAsync(null);
                        }
                        StartBatchAsync(cfg.DelayPaperBreak);
                    }
                    if (nowSpcState.LowerMachineSignal != newState.LowerMachineSignal && nowSpcState.LowerMachineSignal)
                    {
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Temperature = newState.Temperature, DataType = OpticalData.LowerMachineSignal, Remark = "下机" };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    opticalDataSvc.Add(opticalDataList.ToArray());
                    nowSpcStateInfo = newState;
                    StateHandle?.Invoke(Result.Success(Tuple.Create(nowSpcState, opticalDataList.ToArray())));
                    await Task.Delay(1200);
                } while (isRun);
                Console.WriteLine("SpcStatePullLoop stop !");
            });
        }

        public async Task StandardSetAsync(Action<string> step)
        {
            logger.LogInformation("校准测量");

            spcCmd.ExecuteIOCommand(IOFunctionCode.CloseLensCover);
            await Task.Delay(2000);
            step?.Invoke("校准黑");
            commandSet.Standardization(true, cfg.StandardizationDistance);
            step?.Invoke("校准黑完成");
            await Task.Delay(1000);
            spcCmd.ExecuteIOCommand(IOFunctionCode.MoveToWhitePosition);
            await Task.Delay(2000);
            step?.Invoke("校准白");
            commandSet.Standardization(false, cfg.StandardizationDistance);
            await Task.Delay(2000);
            step?.Invoke("校准白完成");
            lastStandardizationTime = DateTime.Now;
            //var result = commandSet.PhotometricDataSingle(cfg.StandardizationDistance);
            //if (result.IsSuccess())
            //{
            //    WhiteByOutStandard = result.Data.Data;
            //    dataDictSvc.SetJson(nameof(WhiteByOutStandard), WhiteByOutStandard);//<float[]
            //    step?.Invoke("校准白保存完成");
            //}
            logger.LogInformation("校准完成");
        }
        //DataDictSvc dataDictSvc=new DataDictSvc();
        float[] WhiteByOutStandard;
    }
}
