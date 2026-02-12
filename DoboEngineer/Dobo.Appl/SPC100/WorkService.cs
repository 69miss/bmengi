using Dobo.Appl.Entity;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.Service;
using Dobo.Appl.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static Dobo.Appl.Utility.INotifyPropertyChangedExt;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dobo.Appl.SPC100;



public class WorkService : IWorkService, IDisposable
{
    DeviceServiceCfg cfg;
    //CommandSet commandSet;
    HTCommand commandSet;
    SPCTcpCommand spcCmd;
    readonly ValueTuple<string, ushort> opticalAddr;
    readonly ValueTuple<string, ushort> spcAddr;
    ILogger logger;

    public long ProductId { get; }
    
   public Action<Result<OpticalData>> PhotometricDataReturnHandle { get; set; }
   public Action<Result<Tuple<SpcStateInfo, OpticalData[]>>> StateHandle { get; set; }
   
    bool SpcPollingIsStop = true;
    bool optIsRun = false;
    //spc是否连接，状态
    //光机是否连接，产品
    //下机防抖时间2s

    //启动测量
    //状态自更新
    //
    public WorkService(Microsoft.Extensions.Logging.ILogger<DeviceService> log, DeviceServiceCfg serviceCfg, long productId = 0)
    {

        opticalAddr = ("192.168.0.55", 10001);
        spcAddr = ("192.168.0.7", 50023);
        logger = log;

        ProductId = productId;
        //timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        opticalDataSvc = new OpticalDataSvc();
        cfg = serviceCfg;
        //nowSpcState.NotifyGet().PropertyChanged += WorkService_PropertyChanged;
        //StateSvc();
        logger.LogInformation("mcode-" + CertifyTool.CertifyInfo.GetMachineCode());
    }

    private void WorkService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e is not PropertyChangedEventArgsExt argsExt)
            return;
        var opticalDataList = new List<OpticalData>();
        if (argsExt.PropertyName == nameof(SpcStateInfo.PaperBreakSignal))
        {
            var oldVal = (bool)argsExt.OldVal;

            if (nowSpcState.PaperBreakSignal)
            {
                OptStop();
                var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignal, Remark = "断纸", Temperature = nowSpcState.Temperature };
                entity.BatchNum = snowflakeIdWorker.NextId();
                opticalDataList.Add(entity);
            }
            else
            {
                var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignalEnd, Remark = "断纸结束", Temperature = nowSpcState.Temperature };
                entity.BatchNum = snowflakeIdWorker.NextId();
                opticalDataList.Add(entity);
                if (optIsRun)
                    OptStartPollingAsync(cfg.DelayPaperBreak);
            }
        }
        else if (argsExt.PropertyName == nameof(SpcStateInfo.LowerMachineSignal))
        {
            var oldVal = (bool)argsExt.OldVal;
            if (nowSpcState.LowerMachineSignal)
            {
                //下机触发
                var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.LowerMachineSignal, Remark = "下机", Temperature = nowSpcState.Temperature };
                entity.BatchNum = snowflakeIdWorker.NextId();
                opticalDataList.Add(entity);
            }
        }

        StateHandle?.Invoke(Result.Success(Tuple.Create(nowSpcState, opticalDataList.ToArray())));
    }



    public async Task StartAsync()
    {
        logger.LogDebug("StateSvcAsync");
        for (int i = 0; i < 3; i++)
        {
            await SpcConnect();
            if (spcConnected)
                break;
            logger.LogWarning("连接spc重试！");
            await Task.Delay(1000);
        }
        if (!spcConnected)
            throw new SystemException("SPC连接失败");
        SpcStartPollingAsync();

        for (int i = 0; i < 3; i++)
        {
            OptConnect();
            if (optConnected)
                break;
            logger.LogWarning("连接opt重试！");
            await Task.Delay(1000);
        }
        if (!optConnected)
            throw new SystemException("OPT连接失败");
        if (cfg.BeginRunStandardization)
        {
            await StandardSetAsync(null);
        }
        optIsRun = true;
        if (!nowSpcState.PaperBreakSignal)
        {
            OptStartPollingAsync(2000);
        }
    }

    private CancellationTokenSource ctsSpc;
    private CancellationTokenSource ctsOpt;
    SpcStateInfo nowSpcState = null;
    bool spcConnected = false;
    async Task SetSpcStateAsync(SpcStateInfo spcStateInfo, bool isConnect = true)
    {
        lock (this)
        {
            if (spcStateInfo == null)
            {
                nowSpcState = null;
                return;
            }
            try
            {
                spcConnected = isConnect;
                var opticalDataList = new List<OpticalData>();
                if (nowSpcState == null)
                {
                    nowSpcState = spcStateInfo;
                }
                if (nowSpcState.PaperBreakSignal != spcStateInfo.PaperBreakSignal)
                {
                    breakBeforeCount = 0;
                    if (spcStateInfo.PaperBreakSignal)
                    {
                        SendMsgInfo(0, "断纸");
                        OptStop();
                        ResetCalendering(false);
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignal, Remark = "断纸", Temperature = nowSpcState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    else
                    {
                        logger.LogDebug(nowSpcState.ToString());
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.PaperBreakSignalEnd, Remark = "断纸结束", Temperature = nowSpcState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                        if (!spcStateInfo.PaperBreakSignal && spcStateInfo.AutoStatus)
                        {
                            ResetCalendering(true);
                            OptStartHandle();
                        }
                    }
                }
                if (nowSpcState.AutoStatus != spcStateInfo.AutoStatus)
                {
                    breakBeforeCount = 0;
                    if (!spcStateInfo.AutoStatus)
                    {
                        SendMsgInfo(0, "自动模式关");
                        OptStop();
                        ResetCalendering(false);
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.AutoStatusClose, Remark = "自动模式关", Temperature = nowSpcState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                    }
                    else
                    {
                        logger.LogDebug(nowSpcState.ToString());
                        var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.AutoStatus, Remark = "自动模式开", Temperature = nowSpcState.Temperature };
                        entity.BatchNum = snowflakeIdWorker.NextId();
                        opticalDataList.Add(entity);
                        if (!spcStateInfo.PaperBreakSignal && spcStateInfo.AutoStatus)
                        {
                            ResetCalendering(true);
                            OptStartHandle();
                        }
                    }
                }
                if (nowSpcState.LowerMachineSignal != spcStateInfo.LowerMachineSignal && spcStateInfo.LowerMachineSignal)
                {
                    var entity = new OpticalData() { ProductId = ProductId, CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, DataType = OpticalData.LowerMachineSignal, Remark = "下机", Temperature = nowSpcState.Temperature };
                    entity.BatchNum = snowflakeIdWorker.NextId();
                    opticalDataList.Add(entity);
                }

                nowSpcState.By(spcStateInfo);

                if (opticalDataList.Count > 0)
                    opticalDataSvc.Add(opticalDataList.ToArray());
                StateHandle?.Invoke(Result.Success(Tuple.Create(spcStateInfo, opticalDataList.ToArray())));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "状态处理异常");
            }
        }

    }

    private async Task OptStartHandle(int? delay=null)
    {
        if (cfg.PaperBreakStandardization == 1)
        {
            await StandardSetAsync(null);
        }
        delay = delay?? cfg.DelayPaperBreak;
        if (optIsRun)
            OptStartPollingAsync(delay.Value);
    }

    public async Task SpcStartPollingAsync()
    {
        logger.LogInformation("spc获取启动");
        SpcPollingIsStop = false;

        ctsSpc = new CancellationTokenSource();
        var token = ctsSpc.Token;
        try
        {
            while (!token.IsCancellationRequested|| !SpcPollingIsStop)
            {
                var startTime = DateTime.Now;
                try
                {
                    //是否连接
                    if (!spcConnected)
                        await SpcConnect();
                    else
                        SetSpcStateAsync(spcCmd.ReadStatus());
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        break;
                    //重连
                    await SpcConnect();
                    logger.LogWarning($"读取失败: {ex.Message}");
                    //await Task.Delay(1000, token);
                    //continue;
                }
                await Task.Delay(1200, token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("spc轮询停止");
        }
    }

    async Task SpcConnect()
    {
        try
        {
            spcCmd?.Dispose();
            spcCmd = new SPCTcpCommand(spcAddr.Item1, spcAddr.Item2);
            spcCmd.Connect();
            await SetSpcStateAsync(spcCmd.ReadStatus(), spcCmd.Connected);
            logger.LogInformation("spc连接成功");
        }
        catch (Exception ex)
        {
            logger.LogWarning("spc连接失败");
            await SetSpcStateAsync(null, false);
        }
    }
    public Task StopAsync()
    {
        SpcStopPolling();
        SendMsgInfo(0, "停止");
        return Task.CompletedTask;
    }
    public void SpcStopPolling()
    {
        SpcPollingIsStop=true;
        logger.LogInformation("SpcStopPolling");
        ctsSpc?.Cancel();
        OptStop();
        spcCmd?.Dispose();
        commandSet?.Dispose();
    }

    bool optConnected = false;
    public async Task OptConnect()
    {
        try
        {
            commandSet?.Dispose();
            commandSet = new HTCommand(opticalAddr.Item1, opticalAddr.Item2, p => logger.LogDebug(p)); // new CommandSet(opticalAddr.Item1, opticalAddr.Item2, p => logger.LogDebug(p));
            if (await commandSet.ResetTcp())
            {
                var rst = commandSet.RemotoMode(true);
                optConnected = rst.IsSuccess();
                logger.LogInformation("opt连接成功");
                return;
            }
        }
        catch (Exception ex)
        {
            optConnected = false;
            logger.LogError(ex, "opt连接报错");
        }
        logger.LogInformation("opt连接失败");
    }
    void OptStop()
    {
        ctsOpt?.Cancel();
    }

    public async Task OptStartPollingAsync(int delayStart = 0)
    {
        optIsRun = true;
        logger.LogDebug("OptStartPollingAsync运行");
        SendMsgInfo(0, "等待测量..");
        lock (this)
        {
            if (ctsOpt != null && !ctsOpt.IsCancellationRequested)
            {
                ctsOpt.Cancel();
                logger.LogWarning("原ctsOpt取消：" + ctsOpt.GetHashCode());
            }
            ctsOpt = new CancellationTokenSource();
        }

        var token = ctsOpt.Token;
        if (delayStart > 0)
        {
            logger.LogDebug("Opt延时启动ms:" + delayStart);
            await Task.Delay(delayStart);
        }
        try
        {
            int optErrCount = 0;
            while (!token.IsCancellationRequested&& !SpcPollingIsStop)
            {
                var optErr = true;
                var startTime = DateTime.Now;
                try
                {
                    SendMsgInfo(0, "测量中...");
                    var wData = await MeasR400_700_10_WAsync(token);
                    token.ThrowIfCancellationRequested();
                    //var bData = await MeasR400_700_10_BAsync(token);
                    var rData = await MeasR400_700_10Async(token);
                    token.ThrowIfCancellationRequested();
                    if (rData.IsSuccess())
                    {
                        optErr = false;
                    }
                    var refTransf = RefTransform(wData.Data, rData.Data, nowSpcState.Temperature, token);
                    if (refTransf != null)
                         MutationClear(wData.Data, refTransf);

                    //CalenderingTransfSave(refTransf);
                    //
                    //  RefTransformByTemperature(  rData.Data, nowSpcState.Temperature, token);
                    await AutoStandardization();
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        break;
                    logger.LogError(ex, $"读取失败: {ex.Message}");

                }
                if (optErr)
                    optErrCount++;
                else
                    optErrCount = 0;
                if (optErrCount > 1)
                {
                    await OptResart();
                    optErrCount = 0;
                }
                SendMsgInfo(0, "等待测量");
                var span = (DateTime.Now - startTime).TotalSeconds;
                var delay = cfg.OpticalWorkInterval - span;
                if (delay < 1)
                    delay = 1;
                await Task.Delay((int)(delay * 1000), token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("opt轮询停止");
        }
    }

    //List<OpticalData> runRecord=new List<OpticalData>();
    int breakBeforeCount = 0;
    float[] lastRData;
    float[] lastWData;
    float[] modifRData;
    List<float[]> mutationRecord=new List<float[]>() ;
    List<float[]> mutationWRecord = new List<float[]>();
    public OpticalData MutationClear(PhotometricData wData,float[] rDataArr) //, PhotometricData rData
    {
        var breakBeforeCountMin = 10;
        var changeRateMin = 0.01;
        var reData = rDataArr;
        if (breakBeforeCount ==0)
            modifRData = null;
        breakBeforeCount++;
        if (breakBeforeCount > breakBeforeCountMin)
        {
            //与上次白板比较
            int[] reflCompIndexs = [5, 8, 10];
            var nowRefls = rDataArr;
            var diffCount = reflCompIndexs.Count(p => Math.Abs(wData.Data[p] - lastWData[p]) / lastWData[p] > changeRateMin);

            //无突变
            if (diffCount < reflCompIndexs.Length)
            {
                reData = NoMutation(wData.Data, nowRefls);
            }
            else
            {
                if (mutationWRecord.Count > 0)
                {
                    logger.LogWarning("有积累突变产生！！！");
                }
                //记录突变量，加入突变列表【原值，突变值1，突变值2】
                //首次

                if (mutationRecord.Count < 1)
                {
                    mutationRecord.Add(lastRData);
                    mutationRecord.Add(nowRefls);
                    mutationWRecord.Add(lastWData);
                    mutationWRecord.Add(wData.Data);
                    logger.LogDebug("MutationClear 首次突变");
                }
                else if (mutationRecord.Count > 1)
                {
                    //后续
                    mutationRecord.Add(nowRefls);
                    logger.LogDebug("MutationClear 持续突变");
                }
                reData = lastRData;
            }
        }
        //更新记录值
        lastRData = reData;//应是处理后的
        lastWData = wData.Data;
        //
        var entity = new OpticalData() { BatchNum = snowflakeIdWorker.NextId(), DataType = OpticalData.MutationClear, DataArrObj = reData.Select(p => (double)p).ToArray(), CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Remark = "突变修正", Temperature = nowSpcState.Temperature };
        var id = opticalDataSvc.Add(entity);
        PhotometricDataReturnHandle?.Invoke(Result.Success(entity));

        return entity;
    }

    private float[]? NoMutation(float[] wData, float[]? nowRefls)
    {
        float[]? reData= nowRefls;
        if (mutationWRecord.Count > 3)
        {
            //大于4根认为已经平稳,清理突变记录,计算修正值
            mutationWRecord.Clear();
            var first = mutationRecord.First();
            var end = mutationRecord.Last();
            modifRData = new float[31];
            for (int i = 0; i < first.Length; i++)
            {
                modifRData[i] =(end[i] - first[i]) * 0.9f;
            }
            logger.LogDebug("MutationClear 大于4根认为已经平稳,清理突变记录,计算修正值:" + FastSerialize.Instance.Serialize(modifRData));
            mutationRecord.Clear();
        }
        else if (mutationWRecord.Count > 0)
        {
            //未平稳则使用最后一次稳定值
            reData = mutationRecord.First();
            mutationWRecord.Add(wData);
            mutationRecord.Add(nowRefls);
            logger.LogDebug("MutationClear 未平稳则使用最后一次稳定值");
            return reData;
        }
        if (mutationWRecord.Count == 0)
        {
            //使用差值修正
            //按照原修正持续
            if (modifRData == null)
            {
                reData = nowRefls;
                logger.LogDebug("MutationClear 差值为空");
                return reData;
            }
            var newRData = nowRefls[..];
            for (int i = 0; i < modifRData.Length; i++)
            {
                newRData[i] -= modifRData[i];
            }
            reData = newRData;
            logger.LogDebug("MutationClear 使用差值修正");
        }
        return reData;
    }

    #region 断纸后未压光修正

    private void CalenderingTransfSave(float[] refTransf)
    {
        var calenArr = CalenderingTransf(refTransf);
        //
        //
        if (calenArr != null)
        {
            var batchNum = snowflakeIdWorker.NextId();
            var entity = new OpticalData() { BatchNum = batchNum, DataType = OpticalData.CalenderingTransf, DataArrObj = calenArr.Select(p => (double)p).ToArray(), CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Remark = "未压光修正", Temperature = nowSpcState.Temperature };

            var id = opticalDataSvc.Add(entity);
            PhotometricDataReturnHandle?.Invoke(Result.Success(entity));
        }
    }

   List<float[]> CalenderingRecords=new List<float[]>();
   readonly  float[] CalenderingTransfVals = [0.0140f, 0.0144f, 0.0145f, 0.0144f, 0.0146f, 0.0148f, 0.0149f, 0.0150f, 0.0151f, 0.0152f, 0.0153f, 0.0154f, 0.0155f, 0.0157f, 0.0158f, 0.0158f, 0.0158f, 0.0158f, 0.0158f, 0.0158f, 0.0158f, 0.0158f, 0.0159f, 0.0159f, 0.0158f, 0.0158f, 0.0159f, 0.0159f, 0.0160f, 0.0157f, 0.0155f];
   float[] dynCalenderingTransfVals = null;
    /// <summary>
    /// 是否进行未压光修正标识
    /// </summary>
    bool isCalenderingTransf = true;
    /// <summary>
    /// 上次反射率临时保存
    /// </summary>
    float[] afterRef = null;
    /// <summary>
    /// 断纸后上次反射率，=断纸后记录的afterRef
    /// </summary>
    float[] breakAfterRef = null;

    void ResetCalendering(bool isTransf)
    {
        CalenderingRecords?.Clear();
        isCalenderingTransf = isTransf;
        breakAfterRef = afterRef;
    }
    /// <summary>
    ///  亚光处理,
    /// </summary>
    float[] CalenderingTransf(float[]  photometricData) {
        //1.断纸时记录最近一次样品反射率
        //2.上纸后，记录第一次样品值,与断纸时比较,如果差值大于固定修正的一半，则不进行后续任何修正操作
        //3.然后按固定值修正,第二次值开始,与第一次原始值对比，如果差值大于固定修正的一半，则停止修正，最多修5次
        //260106：根据文档系统启动不进行压光补充,后续根据上纸首次测得反射率和压光后第一组反射率差值，作为修正值；注意当前没有系统启动时压光判断，导致后面修正值无法确定,算法需要修改!

        if (photometricData == null)
            return null;
        afterRef = photometricData;
        if (CalenderingRecords.Count>10||!isCalenderingTransf|| CalenderingTransfVals==null)
        {
            isCalenderingTransf = false;
            return photometricData;
        }
        
        var first= CalenderingRecords.FirstOrDefault();
        var data =photometricData[..];
        CalenderingRecords.Add(photometricData);

        if (first != null)
        {
            //验证是否关闭修正
            var diff = data[10] - first[10];
            if (diff > CalenderingTransfVals[10] / 2)
            {
                logger.LogDebug("差异判断：停止CalenderingTransf");
                isCalenderingTransf = false;
                return photometricData;
            }

        }
        else if (breakAfterRef != null)
        {
            //断纸后首次校验
            logger.LogDebug("断纸后首次校验：CalenderingTransf");
            var diff = breakAfterRef[10] - data[10];
            if (diff > CalenderingTransfVals[10] / 2)
            {
                logger.LogDebug("断纸后首次判定：停止CalenderingTransf");
                isCalenderingTransf = false;
                return photometricData;
            }
            dynCalenderingTransfVals = new float[31];
            for (int i = 0; i < data.Length; i++)
            {
                dynCalenderingTransfVals[i] = breakAfterRef[i] - data[i];
            }
        }
        else if (breakAfterRef == null) {
            //系统启动第一次
            logger.LogDebug("系统启动第一次：CalenderingTransf");
            dynCalenderingTransfVals = CalenderingTransfVals;
        }
        breakAfterRef = null;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = data[i] + dynCalenderingTransfVals[i];
        }
        logger.LogDebug("执行修正完成CalenderingTransf");
        
        return data;
    }

    #endregion

    public async Task AutoStandardization()
    {
        //校准判断
        if (cfg.AutoStandardizationInterval < 1)
            return;
        if (lastStandardizationTime != null
            && (DateTime.Now - lastStandardizationTime).Value.TotalSeconds < cfg.AutoStandardizationInterval)
        {
            return;
        }
        await StandardSetAsync(null);
        //lastStandardizationTime = DateTime.Now;
    }
    private async Task OptResart()
    {
        try
        {
            logger.LogWarning("重启光机");
            commandSet?.Dispose();
            await Task.Delay(1000);
            spcCmd.ExecuteIOCommand(IOFunctionCode.PowerOffLightMachine);
            await Task.Delay(5000);
            spcCmd.ExecuteIOCommand(IOFunctionCode.PowerOnLightMachine);
            await Task.Delay(3000);
            OptConnect();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "重启光机异常");
        }
    }
    float[][] kbCoef=new float[31][];
    /// <summary>
    /// 根据温度进行修正调节
    /// </summary>
    /// <param name="rData"></param>
    /// <param name="temperature"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    float[] RefTransformByTemperature(PhotometricData rData, float temperature, CancellationToken token) {
        try
        {
            token.ThrowIfCancellationRequested();
            var rRef = rData.Data;
            var newRef = new float[rRef.Length];
            if (temperature > 55)
            {
                temperature = 55;
                SendMsgInfo(2, "传感器温度过高", new object());
            }

            if (temperature < 20)
            {
                temperature = 20;
                SendMsgInfo(2, "传感器温度过低", new object());
            }
            for (int i = 0; i < rRef.Length; i++)
            {
                var rBPred = temperature * kbCoef[i][0] + kbCoef[i][1];
                var rWPred = temperature * kbCoef[i][2] + kbCoef[i][3];
                newRef[i] = (rRef[i] - rBPred) / (rWPred - rBPred) * (cfg.StandardWRefBy30[i] - cfg.WhiteByOutStandard[i]) - cfg.WhiteByOutStandard[i];
            }
            var batchNum = snowflakeIdWorker.NextId();
            var entity = new OpticalData() { BatchNum = batchNum, DataType = OpticalData.FromTemperatureCoefRef, DataArrObj = newRef.Select(p => (double)p).ToArray(), CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Remark = "温度变化率修正", Temperature = temperature };
            var id = opticalDataSvc.Add(entity);
            PhotometricDataReturnHandle?.Invoke(Result.Success(entity));
            return newRef;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RefTransformByTemperature修正异常");
            return null;
        }
    }
    float[] RefTransform(PhotometricData wData, PhotometricData rData, float temperature, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();


            float[] wRef = wData.Data;
            float[] rRef = rData.Data;
            var newRef = new float[cfg.StandardWRefBy30.Length];
            if (cfg.StandardWRefBy30 == null || cfg.StandardWRefBy30.Length < 31 || rData == null || wData == null)
            {
                newRef = rRef;
            }
            else if (CheckWhiteState(wRef, cfg.StandardWRefBy30))
            {
                if (cfg.WhiteByOutStandard == null || cfg.WhiteByOutStandard.Length < 31)
                    cfg.WhiteByOutStandard = cfg.StandardWRefBy30;
                for (int i = 0; i < cfg.StandardWRefBy30.Length; i++)
                {
                    var cvar = cfg.StandardWRefBy30[i] / cfg.WhiteByOutStandard[i];
                    newRef[i] = rRef[i] / wRef[i] * cfg.StandardWRefBy30[i] * cvar;
                    //newRef[i] = rRef[i] / wRef[i] * cfg.StandardWRefBy30[i];
                }
            }
            else
            {
                newRef = rRef;
                //不进行修正，发送错误信息
                MsgInfoHandle?.Invoke(Tuple.Create(1, "基准修正参数异常E30", new object()));
                logger.LogWarning("基准修正参数异常E30:" + FastSerialize.Instance.Serialize(wRef));
            }
            //
            var batchNum = snowflakeIdWorker.NextId();

            //afterRef = newRef[..]; //todo 不能这样加，要看加修正后的还是未修正的，温度修正、压光修正
            //var calenArr = CalenderingTransf(newRef);
            //newRef = calenArr ?? newRef;

            var entity = new OpticalData() { BatchNum = batchNum, DataType = OpticalData.FromStandardWRefBy30, DataArrObj = newRef.Select(p => (double)p).ToArray(), CreateTime = DateTimeOffset.Now, RecordTime = DateTimeOffset.Now, Remark = "反射率修正", Temperature = temperature };

            var id = opticalDataSvc.Add(entity);
            PhotometricDataReturnHandle?.Invoke(Result.Success(entity));
            //
            return newRef;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RefTransform修正异常");
            return null;
        }
    }
    public Action<Tuple< int, string, object>> MsgInfoHandle { get;set; }
    void SendMsgInfo(int type, string msg, object data=null) {
        MsgInfoHandle?.Invoke(Tuple.Create(type, msg, data));
    }

    /// <summary>
    /// 白与基准相差超过5%不进行校准
    /// </summary>
    /// <param name="wRef"></param>
    /// <param name="baseWRef"></param>
    /// <returns></returns>
    bool CheckWhiteState(float[] wRef, float[] baseWRef)
    {
        return true;
        var rangeRate = 0.05f;
        var errNun = 0;
        var refIndex = 10;
        if (wRef[refIndex] > baseWRef[refIndex] * (1 + rangeRate) || wRef[refIndex] < baseWRef[refIndex] * (1 - rangeRate))
            errNun++;
        refIndex = 20;
        if (wRef[refIndex] > baseWRef[refIndex] * (1 + rangeRate) || wRef[refIndex] < baseWRef[refIndex] * (1 - rangeRate))
            errNun++;
        if (errNun > 1)
            return false;
        return true;
    }

   
    async Task<Result<PhotometricData>> MeasR400_700_10Async(CancellationToken token)
    {
        spcCmd.ExecuteIOCommand(IOFunctionCode.OpenLensCover);
        await Task.Delay(2000);
        var re = await sampleMeasAsync(token, OpticalData.R400_700_10, cfg.WorkDistance, "2样品");
       
        spcCmd.ExecuteIOCommand(IOFunctionCode.MoveToWhitePosition);//CloseLensCover
        return re;
    }
    async Task<Result<PhotometricData>> MeasR400_700_10_WAsync(CancellationToken token)
    {
        spcCmd.ExecuteIOCommand(IOFunctionCode.MoveToWhitePosition);
        await Task.Delay(2000);
        var re = await sampleMeasAsync(token, OpticalData.R400_700_10_W, cfg.StandardizationDistance, "1白");
        return re;
    }
    async Task<Result<PhotometricData>> MeasR400_700_10_BAsync(CancellationToken token)
    {
        spcCmd.ExecuteIOCommand(IOFunctionCode.CloseLensCover);
        await Task.Delay(2000);
        var re = await sampleMeasAsync(token, OpticalData.R400_700_10_B, cfg.StandardizationDistance, "1_2黑");
        return re;
    }

    //bool OptIsCancel {
    //    get => ctsOpt == null || ctsOpt.IsCancellationRequested;
    //}
    async Task<Result<PhotometricData>> sampleMeasAsync(CancellationToken token, string type, float distance, string remark = null)
    {
        try
        {
            //
            Result<PhotometricData> data = null;
            var tryCount = 0;
            //返回错误1秒后重试，如果异常，判断原因，则重连，重连后首先进行命令测试，如果响应不正确2次，则断电重启，然后再重连
            var beginTime = DateTimeOffset.Now;
            while (true)
            {
                try
                {
                    if (!optConnected)
                        OptConnect();
                    token.ThrowIfCancellationRequested();
                    data = commandSet.PhotometricDataSingle(distance);
                    if (data.IsSuccess())
                        break;
                    tryCount++;
                }
                catch (Exception ex)
                {
                    token.ThrowIfCancellationRequested();
                    tryCount++;
                }
                if (tryCount >= 2)
                    break;
                logger.LogWarning("测试失败1s后重试:" + tryCount);
                await Task.Delay(1000);
            }
            //
            if (data == null||!data.IsSuccess())
            {
                return new Result<PhotometricData>() { Code = Result.BadGateway, Message = "光机测量异常" };
            }
            AfterMeas(token, type, remark, data, beginTime);
            return data;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "单次测量命令异常");
            return Result.Error<PhotometricData>("单次测量命令异常");
        }
    }

    private Result<PhotometricData> AfterMeas(CancellationToken token, string type, string remark, Result<PhotometricData> data, DateTimeOffset beginTime)
    {
        float temperature = nowSpcState.Temperature;
        //保存数据库//推送
        if (!data.IsSuccess())
        {
            logger.LogWarning("测量数据PhotometricDataSingle返回错误信息：" + data.Message);
        }
        else
        {

            token.ThrowIfCancellationRequested();
            var batchNum = snowflakeIdWorker.NextId();
            var entity = new OpticalData() { BatchNum = batchNum, DataType = type, DataArrObj = data.Data.Data.Select(p => (double)p).ToArray(), CreateTime = beginTime, RecordTime = DateTimeOffset.Now, Remark = remark, Temperature = temperature };
            var id = opticalDataSvc.Add(entity);
            data.Message = batchNum + "";
            PhotometricDataReturnHandle?.Invoke(Result.Success(entity));
        }

        return data;
    }

    readonly static SnowflakeIdWorker snowflakeIdWorker = new SnowflakeIdWorker(2, 0);
    OpticalDataSvc opticalDataSvc;
    
    public DateTime? lastStandardizationTime;
    private bool disposedValue;

    private readonly SemaphoreSlim semapStandardSetAsync = new(1, 1);
    public async Task StandardSetAsync(Action<string> step)
    {
        //logger.LogInformation("校准测量");
        if (!await semapStandardSetAsync.WaitAsync(0))
        {
            logger.LogWarning("StandardSetAsync执行中，跳过此次");
            return;
        }
        try
        {


            lastStandardizationTime = DateTime.Now;
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
            await Task.Delay(1000);
            step?.Invoke("校准白完成");
            lastStandardizationTime = DateTime.Now;
            //logger.LogInformation("校准完成");
        }
        finally
        {
            semapStandardSetAsync.Release(); 
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                SpcStopPolling();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~WorkService()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}


