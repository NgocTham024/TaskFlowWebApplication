using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlowWebApplication.Model
{
    [Table("SocialAccounts")]
    public class SocialAccount
    {
        [Key]
        public int SocialAccountId { get; set; }

        [Required]
        public int UserId { get; set; }

        // Ví dụ: Google, Microsoft
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        // ID duy nhất của người dùng do nhà cung cấp cấp
        [Required]
        [MaxLength(255)]
        public string ProviderUserId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ProviderEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;
    }
}
