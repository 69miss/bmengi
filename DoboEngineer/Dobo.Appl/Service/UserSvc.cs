using Dobo.Appl.Dao;
using Dobo.Appl.Entity;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FreeSql.Internal.GlobalFilter;

namespace Dobo.Appl.Service
{
    public class UserSvc: ServiceBase<User>
    {
        DataMask dataMask=new DataMask("SPCv2");
        public Menu UserMenu(long userId) { 
            throw new NotImplementedException();
        }
        public Result<User> GetByPwd(string longinName, string pwd)
        {

            var user = Query(new { LoginName = longinName }).FirstOrDefault();
            if (user == null)
                return Result.Bad<User>("用户无法找到");
            var pwdStr = dataMask.Decrypt(user.Password);
            if (pwd == pwdStr)
                return Result.Success(user);
            return Result.Bad<User>("密码错误");
        }

        public List<User> List(object dto) {
            return DbUtility.LiteDb.Select<User>().WhereDynamic(dto).IncludeMany(p=>p.Roles).ToList();
        }
        public override int Add(params User[] entitys)
        {
            foreach (var item in entitys)
            {
                if (item.Password != null)
                    item.Password = dataMask.Encrypt(item.Password);
            }
           return DbUtility.LiteDb.Insert(entitys).ExecuteAffrows();
        }
        public long AddIncludeRole(User user)
        {
            if (user.Password != null)
                user.Password = dataMask.Encrypt(user.Password);
            var id=DbUtility.LiteDb.Insert(user).ExecuteIdentity();
            var urArr=user.Roles.Select(p => new UserRole() { UserId = id, RoleId = p.Id }).ToArray();
            if (urArr.Length > 0)
                DbUtility.LiteDb.Insert(urArr).ExecuteAffrows();
            return id;
        }
        public override int Update(User entity)
        {
            var re = DbUtility.LiteDb.Update<User>().SetSource(entity).IgnoreColumns(p => p.Password).ExecuteAffrows();
            DbUtility.LiteDb.Delete<UserRole>(new { UserId = entity.Id }).ExecuteAffrows();
            var urArr = entity.Roles?.Select(p => new UserRole() { RoleId = p.Id, UserId = entity.Id }).ToArray();
            if (urArr != null && urArr.Length > 0)
                DbUtility.LiteDb.Insert(urArr).ExecuteAffrows();
            return re;
        }
        public int SoftDelete(long userId) {

           return DbUtility.LiteDb.Update<User>().Where(p => p.Id == userId).Set(p => p.IsDelete, true).ExecuteAffrows();
        }
        public Result SetPwd(long userId, string pwd, string? oldPwd)
        {
            var user = GetById(userId);
            if (oldPwd != null)
            { 
                var old=dataMask.Decrypt(user.Password );
                if (oldPwd != old)
                    return Result.Bad("密码错误");
            }
            user.Password = dataMask.Encrypt(pwd );
            var re=DbUtility.LiteDb.Update<User>(user).SetSource(user).UpdateColumns(p=>p.Password).ExecuteAffrows();
            if (re > 0)
                return Result.Success();
            return Result.Error("设置失败");
        }
        public override int Save(User entity)
        {
            
            throw new ArgumentException("不支持svae");
        }
        
    }
}
