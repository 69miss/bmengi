using Dobo.Appl.Dao;
using Dobo.Appl.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class ServiceBase<T> where T : EntityBase
    {
        protected readonly IFreeSql fsql = DbUtility.LiteDb;
        public virtual List<T> Query(object dto, Expression<Func<T, Object>>? orderBy =null, bool descending = false, int pageNum = 1, int pageSize = 100)
        {
            return DbUtility.LiteDb.Select<T>().WhereDynamic(dto).OrderByIf(orderBy!=null,orderBy,descending).Page(pageNum, pageSize).ToList();
        }
        public virtual List<T> Query(Expression<Func<T, bool>> exp, Expression<Func<T, Object>>? orderBy = null, bool descending = false, int pageNum = 1, int pageSize = 100)
        {
            return DbUtility.LiteDb.Select<T>().Where(exp).OrderByIf(orderBy != null, orderBy, descending).Page(pageNum, pageSize).ToList();
        }
        public T GetById(object id) {
            return DbUtility.LiteDb.Select<T>(id).First();
        }
        public virtual int Update(T entity) {
           return DbUtility.LiteDb.Update<T>().SetSource(entity).ExecuteAffrows();
        }
        public virtual int Add(params T[] entitys) {
           return DbUtility.LiteDb.Insert(entitys).ExecuteAffrows();
        }
        public virtual long Add(T entity)
        {
            return DbUtility.LiteDb.Insert(entity).ExecuteIdentity();
        }
        public virtual int Delete(T entity) {
            return DbUtility.LiteDb.Delete<T>(entity.Id).ExecuteAffrows();
        }
        public virtual int Save(T entity)
        {
            return DbUtility.LiteDb.InsertOrUpdate<T>().SetSource(entity).ExecuteAffrows();
        }
    }
}
