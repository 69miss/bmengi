using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public class SnowflakeIdWorker
    {
        // 起始时间戳（2020-01-01）
        private  readonly long Twepoch = 1577808000000L;

        // 位长度
        public readonly int WorkerIdBits = 5; // 机器ID位数
        public readonly int DatacenterIdBits = 5; // 数据中心ID位数
        public readonly int SequenceBits = 12; // 序列号位数

        // 最大值
        private readonly long MaxWorkerId ;
        private readonly long MaxDatacenterId;

        // 左移位量
        private int WorkerIdShift;
        private int DatacenterIdShift ;
        private int TimestampLeftShift ;
        private readonly int SequenceMask ;

        private int _sequence = 0;
        private long _lastTimestamp = -1L;

        public long WorkerId { get; protected set; }
        public long DatacenterId { get; protected set; }
        /// <summary>
        /// 64位，每毫秒最多4059个id
        /// </summary>
        /// <param name="workerId">最大32</param>
        /// <param name="datacenterId">最大32</param>
        /// <exception cref="ArgumentException"></exception>
        public SnowflakeIdWorker(long workerId, long datacenterId)
        {
            //初始化
            // 最大值
            MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
            MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
            // 左移位量
            WorkerIdShift = SequenceBits;
            DatacenterIdShift = SequenceBits + WorkerIdBits;
            TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
            SequenceMask = -1 ^ (-1 << SequenceBits);
            NowTimeGen =()=> DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            JoinSeq = p1 => ((p1 - Twepoch) << TimestampLeftShift)
                       | (DatacenterId << DatacenterIdShift)
                       | (WorkerId << WorkerIdShift)
                       | _sequence;
            //
            // 参数检查
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"Worker Id 必须在 0 和 {MaxWorkerId} 之间");
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
                throw new ArgumentException($"Datacenter Id 必须在 0 和 {MaxDatacenterId} 之间");

            WorkerId = workerId;
            DatacenterId = datacenterId;
        }

        /// <summary>
        /// 53位生成方法,精确每秒最多4059个id
        /// </summary>
        /// <param name="workerId">最大32</param>
        /// <exception cref="ArgumentException"></exception>

        public SnowflakeIdWorker(byte workerId)
        {
            //初始化
            DatacenterIdBits = 0;
            Twepoch = Twepoch / 1000;
            // 最大值
            MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
            MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
            // 左移位量
            WorkerIdShift = SequenceBits;
            DatacenterIdShift = SequenceBits + WorkerIdBits;
            TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
            SequenceMask = -1 ^ (-1 << SequenceBits);
            NowTimeGen = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JoinSeq = p1 => ((p1 - Twepoch) << TimestampLeftShift)
                       | (WorkerId << WorkerIdShift)
                       | _sequence;
            //
            // 参数检查
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"Worker Id 必须在 0 和 {MaxWorkerId} 之间");
            WorkerId = workerId;
            DatacenterId = 0;
        }
        private readonly object _lock = new object();

        public long NextId()
        {
            lock (_lock)
            {
                var timestamp = NowTimeGen();

                if (timestamp < _lastTimestamp)
                    throw new Exception($"系统时钟可能出现回拨。拒绝在 {DateTimeOffset.FromUnixTimeSeconds(_lastTimestamp)} 前生成ID。");

                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                        timestamp = TilNextMillis(_lastTimestamp);
                }
                else
                {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                return JoinSeq(timestamp);
            }
        }
        Func<long, long> JoinSeq;

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = NowTimeGen();
            while (timestamp <= lastTimestamp) timestamp = NowTimeGen();
            return timestamp;
        }
        Func<long> NowTimeGen;
       
    }
}
