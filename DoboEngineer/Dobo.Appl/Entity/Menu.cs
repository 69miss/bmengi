using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity
{
    [Table(Name = "sys_menu")]
    public class Menu:EntityBase
    {
        /** 菜单名称 */
        public string? MenuName { get; set; }


        /** 父菜单ID */
        public long ParentId { get; set; }

        /** 显示顺序 */
        public int? OrderNum { get; set; }

        /** 路由地址 */
        public string? Path { get; set; }

        /** 组件路径 */
        public string? Component { get; set; }

        /** 路由参数 */
        public string? Query { get; set; }

        /** 是否为外链（0是 1否） */
        public string? IsFrame { get; set; }

        /** 是否缓存（0缓存 1不缓存） */
        public string? IsCache { get; set; }

        /** 类型（M目录 C菜单 F按钮） */
        public string? MenuType { get; set; }

        /** 显示状态（0显示 1隐藏） */
        public string? Visible { get; set; }

        /** 菜单状态（0正常 1停用） */
        public string? Status { get; set; }

        /** 权限字符串 */
        public string? Perms { get; set; }

        /** 菜单图标 */
        public string? Icon { get; set; }

        /** 子菜单 */
        public List<Menu> Children { get; set; } = new List<Menu>();
    }
}
