using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlowWebApplication.Model
{
    [Table("social_accounts")]
    public class SocialAccount
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("provider_user_id")]
        public string ProviderUserId { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
