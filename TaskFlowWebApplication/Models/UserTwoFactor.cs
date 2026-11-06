using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlowWebApplication.Model
{
    [Table("UserTwoFactors")]
    public class UserTwoFactor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        //secret Key đã được mã hóa bằng Data Protection
        public string? SecretKeyProtected { get; set; }

        //secret Key tạm thời trong quá trình thiết lập 2FA
        public string? PendingSecretKeyProtected { get; set; }

        //false = Tắt 2FA, true = Bật 2FA
        public bool IsEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EnabledAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;

        public long? LastUsedTotpStep { get; set; }
    }
}
