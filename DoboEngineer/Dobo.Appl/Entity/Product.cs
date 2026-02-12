using Dobo.Appl.Utility;
using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "dt_product")]
    public class Product : EntityBase
    {
        public const string LabE = "L|a*|b*|E";
        public string Name { get; set; }
        /// <summary>
        /// 使用分割符分割的产品展示类型： L|A|B|E
        /// </summary>
        public string Types { get; set; }
        public short DataLen()
        {
            switch (Types)
            {
                case LabE: return 4;
                default: return 4;
            }
        }
        /// <summary>
        /// y轴0位,与TypeList对应，json[double]
        /// </summary>
        public string Y0Position { get; set; }
        [Column(IsIgnore = true)]
        public double[] Y0PositionObj
        {
            get
            {
                return FastSerialize.Instance.FromSeparator<double>(Y0Position, ',', '[', ']');
            }
            set
            {
                Y0Position = FastSerialize.Instance.Serialize(value);

            }
        }

        /// <summary>
        /// 实时修正参数
        /// </summary>
        public string Revise { get; set; }
        [Column(IsIgnore = true)]
        public double[] ReviseObj
        {
            get
            {
                if (Revise == null)
                    return null;
                return FastSerialize.Instance.Deserialize<double[]>(Revise);
            }
            set
            {
                Revise = FastSerialize.Instance.Serialize(value);
            }
        }
        /// <summary>
        /// 保存产品界面配置信息
        /// </summary>
        public string ViewCfg { get; set; }

        [Column(IsIgnore = true)]
        public ViewInfo[]? ViewCfgObj
        {
            get
            {
                return FastSerialize.Instance.Deserialize<ViewInfo[]>(ViewCfg);
            }
            set
            {
               ViewCfg= FastSerialize.Instance.Serialize(value);
            }
        }

        public DateTimeOffset Begin { get; set; }
        public DateTimeOffset End { get; set; }
        public DateTimeOffset CreateTime { get; set; }
    }
    public class ViewInfo
    {
        public double YScope { get; set; }
        /// <summary>
        /// [down,up]
        /// </summary>
        public byte[][] RangeColor { get; set; }
        public string Title { get; set; }
    }
}
