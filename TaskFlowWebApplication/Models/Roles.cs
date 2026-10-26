using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlowWebApplication.Model
{
    [Table("roles")]
    public class Roles
    {
        [Key]
        [Column("id")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
