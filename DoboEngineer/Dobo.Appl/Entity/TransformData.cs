using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "dt_transform_data")]
    public class TransformData : EntityBase
    {
        public const string XYZ = "XYZ";
        public const string Lab = "Lab";
        public const string LabE = "LabE";
        public const string R457b = "457b";
        public long BatchNum { get; set; }
        //[Column(MapType = typeof(string), DbType = "datetime")]
        public DateTimeOffset CreateTime { get; set; }
        //[Column(MapType = typeof(string), DbType = "datetime")]
        public DateTimeOffset RecordTime { get; set; }
        public string DataType { get; set; }
        public string DataArr { get; set; }
        public int DataLen { get; set; }
        public double Val1 {  get; set; }
        public double Val2 { get; set; }
        public double Val3 { get; set; }
        public double Val4 { get; set; }
        public double Val5 { get; set; }

        public float Temperature { get; set; }
        public float Distance { get; set; }
        public long? ProductId { get; set; }
        public string? Remark { get;set; }

        //double[]? dataArrObj;
        [Column(IsIgnore = true)]
        public double[]? DataArrObj
        {
            get
            {
                if (DataArr == null)
                    return null;
                var str = DataArr.Trim('[', ']');
                if (string.IsNullOrWhiteSpace(str))
                    return new double[0];
                return str.Split(',').Select(p => double.Parse(p)).ToArray();
            }
            set
            {
                if (value == null)
                {
                    DataArr = null;
                    DataLen = 0;
                    return;
                }
                DataArr = $"[{string.Join(',', value.Select(p => p.ToString()).ToArray())}]";
                DataLen = value.Length;
            }
        }
        public static TransformData CreateBy(OpticalData opticalData) {
            return new TransformData() { BatchNum = opticalData.BatchNum, ProductId = opticalData.ProductId, RecordTime = opticalData.RecordTime, DataArr = opticalData.DataArr, DataLen = opticalData.DataLen, DataType = opticalData.DataType, CreateTime = DateTimeOffset.Now };
        }
    }
}
