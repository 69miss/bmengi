using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    public abstract class EntityBase
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }

        public static double[]? ArrByStr(string dataArr)
        {
            if (dataArr == null)
                return null;
            var str = dataArr.Trim('[', ']');
            if (string.IsNullOrWhiteSpace(str))
                return Array.Empty<double>();
            return str.Split(',').Select(p => double.Parse(p)).ToArray();
        }
    }

}
