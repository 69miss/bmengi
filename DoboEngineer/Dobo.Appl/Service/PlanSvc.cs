using Dobo.Appl.Dao;
using Dobo.Appl.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class PlanSvc
    {
        public List<Plan>  GetPlans(DateTimeOffset date) { 
           return DbUtility.MainDb.Select<Plan>().Where(p => p.SDT.Subtract(date.DateTime).TotalDays <= 0 && p.EDT.Subtract(date.DateTime).TotalDays >= 0).ToList();
        }
        public List<Plan> GetPlans(int pageNum = 1, int pageSize = 100)
        {
            return new List<Plan>() {new Plan() };
            return DbUtility.MainDb.Select<Plan>().OrderByDescending(p=>p.EDT).OrderByDescending(p=>p.PlanId).Page(pageNum,pageSize).ToList();
        }
        public Plan GetPlan(long id)
        {
            return DbUtility.MainDb.Select<Plan>(id).First();
        }

        public List<PlanRecord> GetPlanRecords(long planId, int idxBegin, int idxEnd)
        {
            return new List<PlanRecord>();
            return DbUtility.MainDb.Select<PlanRecord>().Where(p => p.PlanId == planId && p.Idx >= idxBegin && p.Idx <= idxEnd).ToList();
        }
        public List<PlanRecord> GetPlanRecordByPage(long planId, int pageNum = 1, int pageSize=100,DateTime? end=null)
        {
            return new List<PlanRecord>() {new PlanRecord() };
            return DbUtility.MainDb.Select<PlanRecord>().Where(p => p.PlanId == planId).WhereIf(end!=null,p=>p.Dt<=end).OrderByDescending(p => p.Idx).Page(pageNum, pageSize).ToList();
        }
    }
}
