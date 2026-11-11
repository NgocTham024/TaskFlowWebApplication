using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpires { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public byte IsEmailVerified { get; set; }

    public byte IsActive { get; set; }

    public byte TwoFactorEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();

    public virtual ICollection<Notification> NotificationActors { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetToken1> PasswordResetToken1s { get; set; } = new List<PasswordResetToken1>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<SocialAccount1> SocialAccount1s { get; set; } = new List<SocialAccount1>();

    public virtual ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();

    public virtual ICollection<TaskAssignee> TaskAssigneeAssignedByNavigations { get; set; } = new List<TaskAssignee>();

    public virtual ICollection<TaskAssignee> TaskAssigneeUsers { get; set; } = new List<TaskAssignee>();

    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

    public virtual ICollection<Tasks> Tasks { get; set; } = new List<Tasks>();

    public virtual ICollection<TwoFactorLoginChallenge> TwoFactorLoginChallenges { get; set; } = new List<TwoFactorLoginChallenge>();

    public virtual ICollection<UserTwoFactor> UserTwoFactors { get; set; } = new List<UserTwoFactor>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
