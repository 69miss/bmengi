using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    public class OperationRecord:EntityBase
    {
        public string OpType { get;set; }
        public DateTimeOffset CreateTime { get;set; }
        
        public string Val { get;set; }
        public string Val2 { get;set; }
        public string Val3 { get;set; }
    }
}
