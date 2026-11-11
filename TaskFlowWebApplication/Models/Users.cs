using System.ComponentModel.DataAnnotations;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Model
{
    public class Users
    {
        [Key]
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpires { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Bổ sung các trường nghiệp vụ từ bản -ok.
        public string? FullName { get; set; }

        public string? Avatar { get; set; }

        public byte IsActive { get; set; } = 1;

        public byte TwoFactorEnabled { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ActivityLog> ActivityLogs { get; set; }
            = new List<ActivityLog>();

        public virtual ICollection<Board> Boards { get; set; }
            = new List<Board>();

        public virtual ICollection<Notification> NotificationActors { get; set; }
            = new List<Notification>();

        public virtual ICollection<Notification> NotificationUsers { get; set; }
            = new List<Notification>();

        public virtual ICollection<TaskAssignee> TaskAssigneeAssignedByNavigations { get; set; }
            = new List<TaskAssignee>();

        public virtual ICollection<TaskAssignee> TaskAssigneeUsers { get; set; }
            = new List<TaskAssignee>();

        public virtual ICollection<TaskComment> TaskComments { get; set; }
            = new List<TaskComment>();

        public virtual ICollection<Tasks> Tasks { get; set; }
            = new List<Tasks>();

        public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; }
            = new List<WorkspaceMember>();

        public virtual ICollection<Workspace> Workspaces { get; set; }
            = new List<Workspace>();

        public virtual ICollection<Role> Roles { get; set; }
            = new List<Role>();

        // Giữ riêng các bảng snake_case khác với bảng auth hiện tại.
        public virtual ICollection<LegacyPasswordResetToken> LegacyPasswordResetTokens { get; set; }
            = new List<LegacyPasswordResetToken>();

        public virtual ICollection<LegacySocialAccount> LegacySocialAccounts { get; set; }
            = new List<LegacySocialAccount>();

        public virtual ICollection<LegacyRefreshToken> LegacyRefreshTokens { get; set; }
            = new List<LegacyRefreshToken>();
    }
}