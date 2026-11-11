using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Models;
using TaskStatus = TaskFlowWebApplication.Models.TaskStatus;

namespace TaskFlowWebApplication.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Board> Boards { get; set; }

    public virtual DbSet<BoardColumn> BoardColumns { get; set; }

    public virtual DbSet<Label> Labels { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<PasswordResetToken1> PasswordResetTokens1 { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SocialAccount> SocialAccounts { get; set; }

    public virtual DbSet<SocialAccount1> SocialAccounts1 { get; set; }

    public virtual DbSet<Tasks> Tasks { get; set; }

    public virtual DbSet<TaskAssignee> TaskAssignees { get; set; }

    public virtual DbSet<TaskComment> TaskComments { get; set; }

    public virtual DbSet<TaskList> TaskLists { get; set; }

    public virtual DbSet<TaskPriority> TaskPriorities { get; set; }

    public virtual DbSet<TaskStatus> TaskStatuses { get; set; }

    public virtual DbSet<TwoFactorLoginChallenge> TwoFactorLoginChallenges { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserTwoFactor> UserTwoFactors { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("activity_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.BoardId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("board_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("description");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(30)
                .HasColumnName("entity_type");
            entity.Property(e => e.TaskId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("task_id");
            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("user_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Board).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.BoardId)
                .HasConstraintName("FK_activity_logs_boards");

            entity.HasOne(d => d.Task).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK_activity_logs_tasks");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_activity_logs_users");

            entity.HasOne(d => d.Workspace).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_activity_logs_workspaces");
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("boards");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Boards)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_boards_users");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Boards)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_boards_workspaces");
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.ToTable("board_columns");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BoardId).HasColumnName("board_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Board).WithMany(p => p.BoardColumns)
                .HasForeignKey(d => d.BoardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_board_columns_boards");
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("labels");

            entity.HasIndex(e => e.WorkspaceId, "IX_labels").IsUnique();

            entity.HasIndex(e => e.Name, "IX_labels_1").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("#808080")
                .HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithOne(p => p.Label)
                .HasForeignKey<Label>(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_labels_workspaces");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActorId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("actor_id");
            entity.Property(e => e.BoardId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("board_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message)
                .HasMaxLength(500)
                .HasColumnName("message");
            entity.Property(e => e.TaskId)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("task_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.NotificationActors)
                .HasForeignKey(d => d.ActorId)
                .HasConstraintName("FK_notifications_users1");

            entity.HasOne(d => d.Board).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.BoardId)
                .HasConstraintName("FK_notifications_boards");

            entity.HasOne(d => d.Task).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK_notifications_tasks");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notifications_users");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");

            entity.HasIndex(e => e.Id, "U_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pass_user");
        });

        modelBuilder.Entity<PasswordResetToken1>(entity =>
        {
            entity.ToTable("PasswordResetTokens");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetToken1s)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetTokens_users");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.Id, "U_token_hash").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
            entity.Property(e => e.RevokedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_id");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasIndex(e => e.Id, "U_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SocialAccount>(entity =>
        {
            entity.ToTable("social_accounts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderUserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("provider_user_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SocialAccounts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_social_user");
        });

        modelBuilder.Entity<SocialAccount1>(entity =>
        {
            entity.HasKey(e => e.SocialAccountId);

            entity.ToTable("SocialAccounts");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProviderEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ProviderUserId)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.SocialAccount1s)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_SocialAccounts_users");
        });

        modelBuilder.Entity<Tasks>(entity =>
        {
            entity.ToTable("tasks");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("due_date");
            entity.Property(e => e.ListId).HasColumnName("list_id");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("start_date");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tasks_users");

            entity.HasOne(d => d.List).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tasks_task_lists");

            entity.HasOne(d => d.Priority).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tasks_tasks1");

            entity.HasOne(d => d.Status).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tasks_tasks");

            entity.HasMany(d => d.Labels).WithMany(p => p.Tasks)
                .UsingEntity<Dictionary<string, object>>(
                    "TaskLabel",
                    r => r.HasOne<Label>().WithMany()
                        .HasForeignKey("LabelId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_task_labels_labels"),
                    l => l.HasOne<Tasks>().WithMany()
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_task_labels_tasks"),
                    j =>
                    {
                        j.HasKey("TaskId", "LabelId");
                        j.ToTable("task_labels");
                        j.IndexerProperty<int>("TaskId").HasColumnName("task_id");
                        j.IndexerProperty<int>("LabelId").HasColumnName("label_id");
                    });
        });

        modelBuilder.Entity<TaskAssignee>(entity =>
        {
            entity.HasKey(e => new { e.TaskId, e.UserId });

            entity.ToTable("task_assignees");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.TaskAssigneeAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_assignees_users1");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAssignees)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_assignees_tasks");

            entity.HasOne(d => d.User).WithMany(p => p.TaskAssigneeUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_assignees_users");
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("task_comments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContentCmt).HasColumnName("content_cmt");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskComments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_comments_tasks");

            entity.HasOne(d => d.User).WithMany(p => p.TaskComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_comments_users");
        });

        modelBuilder.Entity<TaskList>(entity =>
        {
            entity.ToTable("task_lists");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ColumnId).HasColumnName("column_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Column).WithMany(p => p.TaskLists)
                .HasForeignKey(d => d.ColumnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_lists_board_columns");
        });

        modelBuilder.Entity<TaskPriority>(entity =>
        {
            entity.ToTable("task_priorities");

            entity.HasIndex(e => e.Level, "UN_level").IsUnique();

            entity.HasIndex(e => e.Name, "UN_name_pri").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("description");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TaskStatus>(entity =>
        {
            entity.ToTable("task_statuses");

            entity.HasIndex(e => e.Name, "UN_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TwoFactorLoginChallenge>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserId");

            entity.HasIndex(e => e.Id, "UN_Id").IsUnique();

            entity.HasIndex(e => e.TokenHash, "UN_hash").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.TwoFactorLoginChallenges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_TwoFactorLoginChallenges_users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.UserId, "U_email").IsUnique();

            entity.HasIndex(e => e.UserId, "U_username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Avatar)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue((byte)1)
                .HasColumnName("is_active");
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<UserRole>(
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PK,FK_id_role"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PK,FK_role_user"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("user_roles");
                        j.Property(e => e.UserId).HasColumnName("user_id");
                        j.Property(e => e.RoleId).HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<UserTwoFactor>(entity =>
        {
            entity.HasIndex(e => e.Id, "UN_User").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EnabledAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PendingSecretKeyProtected)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SecretKeyProtected)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.UserTwoFactors)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserTwoFactors_users");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Workspaces)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_workspaces_workspaces");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("workspace_members");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("joined_at");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_workspace_members_roles");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_workspace_members_users");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_workspace_members_workspaces");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
