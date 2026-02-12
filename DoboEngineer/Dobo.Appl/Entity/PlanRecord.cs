using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{

    [Table(Name = "DT_PLAN_LIST")]
    public class PlanRecord
    {
        public int PlanId { get; set; }
        [Column(IsIdentity = true, IsPrimary = true)]
        public int Idx { get; set; }
        public DateTime Dt { get; set; }
        public double L { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double E { get; set; }

        [Column(IsIgnore = true)]
        public int Status
        {
            get
            {
                return L >= 90 ? 1 : 0;
            }
        }
    }
}
