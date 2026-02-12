using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{

    [Table(Name ="sys_dict")]
    public class DataDict: EntityBase
    {
        public long? ParentId { get; set; }
        public string Name { get; set; }
        public string TargetType { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        [Column(StringLength = 5000)]
        public string Value { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }

        public DateTime CreateTime { get;set; }= DateTime.Now;
        public DateTime? UpdateTime { get; set; }

    }
}
