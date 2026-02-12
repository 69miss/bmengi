using Dobo.Appl.Entity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class TransformDataSvc : ServiceBase<TransformData>
    {
        public List<Tuple<TransformData, OpticalData>> OpticalView(Expression<Func<TransformData, OpticalData, bool>> exp)
        {
            var data = fsql.Select<TransformData, OpticalData>()
                 .LeftJoin((a, b) => b.BatchNum > 0 && a.BatchNum == b.BatchNum)
                 .Where(exp).ToList((a, b) => new { a, OpDataArr = b==null?null:b.DataArr, OpRemark = b==null?null:b.Remark ,tempr=b.Temperature});
            var tu = data.Select(p => Tuple.Create(p.a,new OpticalData() { DataArr=p.OpDataArr,Remark=p.OpRemark,Temperature=p.a.Temperature })).ToList();
            return tu;
        }
        public List<TransformData> GetByTime(DateTimeOffset dateTimeOffset, DateTimeOffset? end=null, int count = 6000)
        {
            if (end == null)
            {
                var aroundCount = count / 2;
                var beforeCount = aroundCount;
                if (aroundCount * 2 < count)
                    beforeCount++;
                var beforeQy = fsql.Select<TransformData>()
                .Where(p => p.CreateTime <= dateTimeOffset)
                .OrderByDescending(p => p.CreateTime).Limit(beforeCount);
                var afterQy = fsql.Select<TransformData>()
                    .Where(p => p.CreateTime > dateTimeOffset)
                    .OrderBy(p => p.CreateTime).Limit(aroundCount);
                var qy = beforeQy.UnionAll(afterQy).OrderBy(p => p.CreateTime);
                return qy.ToList();
            }
            return fsql.Select<TransformData>().Where(p => p.CreateTime >= dateTimeOffset && p.CreateTime <= end).OrderBy(p => p.CreateTime).Limit(count).ToList();
        }
    }
    
}
