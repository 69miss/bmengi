using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "DT_PLAN")]
    public class Plan
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public int PlanId { get; set; }
        public string StuffNo { get; set; }
        public double L0 { get; set; }
        public double A0 { get; set; }
        public double B0 { get; set; }
        public double W0 { get; set; }
        public double DL { get; set; }
        public double DA { get; set; }
        public double DB { get; set; }
        public double DW { get; set; }
        public DateTime SDT { get; set; }
        public DateTime EDT { get; set; }
    }
}
