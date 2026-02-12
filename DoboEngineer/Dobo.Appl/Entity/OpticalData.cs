using Dobo.Appl.SPC100;
using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "dt_optical_data")]
    public class OpticalData:EntityBase
    {
        public const string R400_700_10 = "R400_700_10";
        public const string FromStandardWRefBy30 =nameof(FromStandardWRefBy30);
        public const string FromTemperatureCoefRef = nameof(FromTemperatureCoefRef);
        public const string CalenderingTransf = nameof(CalenderingTransf);
        public const string MutationClear = nameof(MutationClear);
        /// <summary>
        /// 白测量
        /// </summary>
        public const string R400_700_10_W = "R400_700_10_W";
        public const string R400_700_10_B =nameof(R400_700_10_B);
        public const string CalibrGreen_XYZ = "CalibrGreen_XYZ";
        public const string PaperBreakSignal = "PaperBreakSignal";
        public const string PaperBreakSignalEnd = "PaperBreakSignalEnd";
        public const string LowerMachineSignal = "LowerMachineSignal";
        public const string Standardization = "Standardization";
        public const string AutoStatusClose = nameof(SpcStateInfo.AutoStatus)+"Close";
        public const string AutoStatus = nameof(SpcStateInfo.AutoStatus);
        //
        public const string LabRevise=nameof(LabRevise);
        [Column(IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }
        public long BatchNum { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset RecordTime { get; set; }
        public string DataType { get; set; }
        public string DataArr { get; set; }
        public int DataLen { get; set; }
       
        public float Temperature { get;set; }
        public float Distance { get; set; }
        public long? ProductId { get;set; }
        public string Remark { get; set; }

        [Column(IsIgnore = true)]
        public double[]? DataArrObj
        {
            get
            {
                return ArrByStr(DataArr);
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

      
    }
}
