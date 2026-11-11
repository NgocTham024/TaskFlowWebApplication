using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskFlowWebApplication.Model
{
    [Table("user_roles")]
    [PrimaryKey(nameof(UserId), nameof(RoleId))]
    public class UserRoles
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }
    }
}
