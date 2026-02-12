using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Entity;

[Table(Name = "sys_role")]
public class Role:EntityBase
{
    /** 角色名称 */
    public string? RoleName { get; set; }

    /** 角色权限(admin/common) */
    public string? RoleKey { get; set; }

    /** 角色排序 */
    public int RoleSort { get; set; }

    /** 数据范围（1：所有数据权限；2：自定义数据权限；3：本部门数据权限；4：本部门及以下数据权限；5：仅本人数据权限） */
    public string? DataScope { get; set; }



    /** 角色状态（0正常 1停用） */
    public string? Status { get; set; }

    /** 删除标志（0代表存在 2代表删除） */
    public string? DelFlag { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark{get; set;}

    public List<Menu> Menus { get; set; }

}
