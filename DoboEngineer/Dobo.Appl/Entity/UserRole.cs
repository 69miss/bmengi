using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name ="sys_user_role")]
    public class UserRole : EntityBase
    {
        /// <summary>
        /// 用户ID (user_id)
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 角色ID (role_id)
        /// </summary>
        public long RoleId { get; set; }
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
