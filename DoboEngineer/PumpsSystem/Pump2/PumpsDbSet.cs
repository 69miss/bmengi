using Dobo.Appl.Service;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;
internal class PumpsDbSet
{
    public static Tuple<List<IDataItemProp>, List<PumpModel>> Pumps4CfgDef(bool isCheck=true)
    {
        //查询是否存在
        //创建对象
        //保存到数据库
        var dataDictSvc = new DataDictSvc();
        var root = dataDictSvc.GetTreeByName("PumpsCfg");
        if (isCheck)
        {
            if (root != null)
                return null;
        }

        var pumps = new List<PumpModel>();
        var tree = Tuple.Create(new List<IDataItemProp>(), pumps);
        var idIndex = 1;
        pumps.Add(new PumpModel { Id = "" + idIndex++, Name = "1#", DisplayColor = "#0078D4", MaxFlow = 120, MaxStroke = 100, MinStroke = 10, ProtectionThreshold = 150 });
        pumps.Add(new PumpModel { Id = "" + idIndex++, Name = "2#", DisplayColor = "#107C10", MaxFlow = 80.5, MaxStroke = 80, MinStroke = 5, ProtectionThreshold = 90 });
        pumps.Add(new PumpModel { Id = "" + idIndex++, Name = "3#", DisplayColor = "#D13438", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });
        pumps.Add(new PumpModel { Id = "" + idIndex++, Name = "4#", DisplayColor = "#FF8C00", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });
        //
        var registerMap = new Dictionary<string, IDataItemProp>();
        IDataItemProp GetOrCreateRegister(IConvertible addr, string name, bool canWrite)
        {
            var addrStr = addr.ToString(null);
            if (!registerMap.TryGetValue(addrStr, out var reg))
            {
                reg = new DataItemProp<short>()
                {
                    Address = addrStr,
                    Name = name,
                    CanWrite = canWrite,
                };// CreateItem(addr, name, canWrite);
                registerMap[addrStr] = reg;
            }
            return reg;
        }

        // 1. 全局独立点位
        GetOrCreateRegister(40001, "心跳", false);
        ushort baseAddrPV = 40005;
        ushort baseAddrStrokeLimit = 40033;
        ushort modeAddr = 40004;
        var globalModeReg = GetOrCreateRegister(modeAddr, "控制模式", true);
        // 2. 遍历每个泵，为其构造专属的 PumpModbusContext
        foreach (var pm in pumps)
        {
            var pNum = int.Parse(pm.Id);
            var pumpIndex = pNum - 1;

            // 备注：如果以后需要在 PumpModel 中配置任意点位，可替换如下计算方式
            // 例如：ushort statusAddr = pumpVM.Cfg.StatusAddress ?? (ushort)(40002 + pumpIndex / 4);

            // --- 状态位 (兼容旧版：每4个泵占1个寄存器) ---
            ushort statusAddr = (ushort)(40002 + pumpIndex / 4);
            var statusReg = GetOrCreateRegister(statusAddr, $"状态反馈_{statusAddr}", false);

            // --- 控制位 (兼容旧版：每16个泵占1个寄存器) ---
            ushort ctlAddr = (ushort)(40003 + pumpIndex / 16);
            var ctlReg = GetOrCreateRegister(ctlAddr, $"控制字_{ctlAddr}", true);

            // --- 模式位 (兼容旧版：共用40004) ---


            var ctx = new PumpItem
            {
                IsRemote = new DataItemBitMap(statusReg, (pumpIndex % 4), $"{pNum}-远程模式"),
                IsFault = new DataItemBitMap(statusReg, (pumpIndex % 4) + 4, $"{pNum}-错误状态"),
                IsRunning = new DataItemBitMap(statusReg, (pumpIndex % 4) + 8, $"{pNum}-运行状态"),
                CtlRunning = new DataItemBitMap(ctlReg, pumpIndex % 16, $"{pNum}-运行状态设置"),
                ModeFlow = new DataItemBitMap(globalModeReg, 0, $"{pNum}-流量模式"),
                ModeManual = new DataItemBitMap(globalModeReg, 1, $"{pNum}-手动模式"),

                FreqPV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-频率", false),
                StrokePV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-冲程", false),
                FlowPV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-流量", false),
                MaxFlowSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-最大流量", true),
                FreqSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-频率设定", true),
                StrokeSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-冲程设定", true),
                FlowSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-流量设定", true),

                MaxStroke = GetOrCreateRegister(baseAddrStrokeLimit++, $"泵{pNum}-最大冲程", true),
                MinStroke = GetOrCreateRegister(baseAddrStrokeLimit++, $"泵{pNum}-最小冲程", true)
            };
            pm.AddressInfo = ctx;
            //pm.InjectModbusContext(ctx);
        }


        var pArr = registerMap.Values.OrderBy(p => p.Address).ToArray();
        tree.Item1.AddRange(pArr);
        //
        if (root != null)
        {
            dataDictSvc.DeleteByParentId(root.Id);
            dataDictSvc.Delete(root);
        }
        root = new Dobo.Appl.Entity.DataDict();
        root.Name = "PumpsCfg";
        root.Value2 = FastSerialize.Instance.Serialize(pArr);
        root.Id = dataDictSvc.Add(root);
        var arr = pumps.Select(p => p.ToDict(root.Id)).ToArray();
        dataDictSvc.Add(arr);
        return tree;
    }
    public static Tuple<IDataItemProp[], PumpModel[]> GetCfg(string cfgName)
    {
        var dataDictSvc = new DataDictSvc();
        var root = dataDictSvc.GetTreeByName(cfgName);
        if (root == null)
            return null;
        var defCfg = Pump6CfgDef();
        var dataItems = defCfg.Item1;//PumpModel.GetItemsByValue2(root).Cast<IDataItemProp>().ToArray();
        var pumps = root.Childs.Select(p => PumpModel.From(p)).ToArray();
        pumps.Each(p =>
        {
            var defP = defCfg.Item2.First(x => x.Id == p.Id);
            p.AddressInfo = defP.AddressInfo;
        });
        return Tuple.Create(dataItems, pumps);
    }
    public static Tuple<IDataItemProp[], PumpModel[]> Pumps6CfgDbInit(bool isCheck = true)
    {
        //查询是否存在
        //创建对象
        //保存到数据库
        var dataDictSvc = new DataDictSvc();
        var root = dataDictSvc.GetTreeByName("Pumps6Cfg");
        if (isCheck)
        {
            if (root != null)
                return null;
        }
        var tree=Pump6CfgDef();
        var pumps=tree.Item2;
        var pArr=tree.Item1;
        //
        if (root != null)
        {
            dataDictSvc.DeleteByParentId(root.Id);
            dataDictSvc.Delete(root);
        }
        root = new Dobo.Appl.Entity.DataDict();
        root.Name = "Pumps6Cfg";
        root.Value2 = FastSerialize.Instance.Serialize(pArr);
        root.Id = dataDictSvc.Add(root);
        var arr = pumps.Select(p => p.ToDict(root.Id)).ToArray();
        dataDictSvc.Add(arr);
        return tree;
    }

    public static Tuple<IDataItemProp[], PumpModel[]> Pump6CfgDef()
    {
       var pumps = new List<PumpModel>();
       
        var colors = new PumpCfgViewModel().AvailableColors;
        for (int i = 0; i < 6; i++)
        {
            pumps.Add(new PumpModel { Id = $"{i + 1}", Name = $"{i+1}#", DisplayColor = colors[i % colors.Count].Item2, MaxFlow = 2, MaxStroke = 25000, MinStroke = 2200, ProtectionThreshold = 150 });
        }
        //
        var registerMap = new Dictionary<string, IDataItemProp>();
        IDataItemProp GetOrCreateRegister3(IConvertible addr, string name, IConvertible valDef, bool canWrite = false)
        {

            var addrStr = addr.ToString(null);
            if (!registerMap.TryGetValue(addrStr, out var reg))
            {
                reg = new DataItemProp(valDef)
                {
                    Address = addrStr,
                    Name = name,
                    CanWrite = canWrite,
                };
                registerMap[addrStr] = reg;
            }
            return reg;
        }
        DataItemProp<T> GetOrCreateRegister<T>(IConvertible addr, string name, T valDef, bool canWrite = false) where T : IConvertible
        {
            var addrStr = addr.ToString(null);
            if (!registerMap.TryGetValue(addrStr, out var reg))
            {
                reg = new DataItemProp<T>()
                {
                   
                    Address = addrStr,
                    Name = name,
                    CanWrite = canWrite,
                    Value = valDef,
                };
                registerMap[addrStr] = reg;
            }
            return reg as DataItemProp<T>;
        }
        // 1. 全局独立点位
        var dbPrefix = "DB12.DB";
        GetOrCreateRegister("DB12.DBX0.0", "心跳", false);
        GetOrCreateRegister("DB12.DBX0.1", "报警复位", false, true);
        var globalModeReg = GetOrCreateRegister<short>("DB12.DBW2", "控制模式", 1, true);
        
        ushort baseAddrPV = 2;
        // 2. 遍历每个泵，为其构造专属的 PumpModbusContext
        foreach (var pm in pumps)
        {
            var pNum = int.Parse(pm.Id);
            var pumpIndex = pNum - 1;
            var ctx = new PumpItem() { PumpId = pm.Id };
            ctx.IsRemote = new DataItemToBitMap(globalModeReg, null, 4);
            ctx.ModeFlow = new DataItemToBitMap(globalModeReg, 1);
            ctx.ModeManual = new DataItemToBitMap(globalModeReg, 2);
            ctx.MaxFlowSV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-标定流量", 2, true);
            ctx.FreqPV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-频率反馈", 0);
            ctx.StrokePV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-冲程反馈", 0);
            ctx.FlowPV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-流量反馈", 0);
            ctx.FreqSV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-目标频率", 0, true);
            ctx.StrokeSV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-目标冲程", 0, true);
            ctx.FlowSV = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-目标流量", 0, true);
            ctx.MaxStroke = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-冲程最大限值", 25000, true);
            ctx.MinStroke = GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-冲程最小限值", 2200, true);
            GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-电流反馈", 0);
            GetOrCreateRegister<short>($"{dbPrefix}W{baseAddrPV += 2}", $"泵{pNum}-电压反馈", 0);
            baseAddrPV += 2;
            ctx.IsRunning = GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.0", $"泵{pNum}-运行反馈", false);
            ctx.IsFault = GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.1", $"泵{pNum}-故障反馈", false);
            ctx.CtlRunning = GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.2", $"泵{pNum}-泵启动控制", false, true);
            GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.3", $"泵{pNum}-冲程正转控制", false);
            GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.4", $"泵{pNum}-冲程反转控制", false);
            GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.5", $"泵{pNum}-泵停止控制", false);
            GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.6", $"泵{pNum}-冲程超时报警", false);
            GetOrCreateRegister($"{dbPrefix}X{baseAddrPV}.7", $"泵{pNum}-次数超限报警", false);
            pm.AddressInfo = ctx;
        }


        var pArr = registerMap.Values.ToArray();
       var tree = Tuple.Create(pArr, pumps.ToArray());
        return tree;
    }
}

