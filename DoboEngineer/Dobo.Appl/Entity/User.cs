using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "sys_user")]
    public class User:EntityBase
    {
        public string Name{ get;set;}
        public string LoginName { get;set;}
        public string Type { get; set;}
        /// <summary>
        /// 0停用，1正常
        /// </summary>
        public int Status { get;set;}
        public string Password{ get;set;}
        public string PowerList {  get;set;}
        [Navigate(ManyToMany =typeof(UserRole))]
        public List<Role> Roles { get; set;}
        
        public bool IsDelete { get;set;}
        public DateTimeOffset CreateTime { get; set; } = DateTimeOffset.Now;
    }
}
