using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;
namespace TaskFlowWebApplication.Services
{
    public class WorkspaceService: IWorkspaceService
    {
        private readonly AppDbContext _context;
        private readonly int _ownerRoleId;
        private readonly int _memberRoleId;
        public WorkspaceService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _ownerRoleId = configuration.GetValue<int>("WorkspaceRoles:OwnerRoleId");
            _memberRoleId = configuration.GetValue<int>("WorkspaceRoles:MemberRoleId");
            if (_ownerRoleId <= 0 || _memberRoleId <= 0 || _ownerRoleId == _memberRoleId)
            {
                throw new InvalidOperationException("Workspace OwnerRoleId và MemberRoleId chưa hợp lệ.");
            }
        }
        public async Task<WorkspaceResponseDTO> CreateWorkspaceAsync(CreateWorkspaceDTO createWorkspaceDTO, int userId)
        {
            var now = DateTime.UtcNow;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var workspace = new Workspace
            {
                Name = createWorkspaceDTO.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(createWorkspaceDTO.Description)? null: createWorkspaceDTO.Description.Trim(),
                CreatedBy = userId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();
            var owner = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                RoleId = _ownerRoleId,
                JoinedAt = now
            };
            _context.WorkspaceMembers.Add(owner);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new WorkspaceResponseDTO
            {
                WorkspaceId = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                CreatedAt = workspace.CreatedAt,
                UpdatedAt = workspace.UpdatedAt,
                MemberCount = 1,
                MyRole = "Owner"
            };
        }
        public async Task<List<WorkspaceResponseDTO>> GetWorkspacesAsync(int userId)
        {
            var workspaces = await _context.WorkspaceMembers.AsNoTracking().Where(wm => wm.UserId == userId)
                .Select(wm => new WorkspaceResponseDTO
                {
                    WorkspaceId = wm.Workspace.Id,
                    Name = wm.Workspace.Name,
                    Description = wm.Workspace.Description,
                    CreatedAt = wm.Workspace.CreatedAt,
                    UpdatedAt = wm.Workspace.UpdatedAt,
                    MemberCount = wm.Workspace.WorkspaceMembers.Count,
                    MyRole = wm.RoleId == _ownerRoleId ? "Owner" : "Member"
                }).OrderByDescending(w => w.UpdatedAt).ToListAsync();
            return workspaces;
        }
        private async Task<bool> IsMemberAsync(int workspaceId,int userId)
        {
            return await _context.WorkspaceMembers.AnyAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == userId);
        }
        public async Task<WorkspaceResponseDTO?> GetWorkspacesByIdAsync(int workspaceId, int userId)
        {
            bool isMember = await IsMemberAsync(workspaceId, userId);
            if (!isMember)
            {
                return null;
            }
            var workspace = await _context.Workspaces.AsNoTracking().Where(w => w.Id == workspaceId)
                .Select(w => new WorkspaceResponseDTO
                {
                    WorkspaceId = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                    MemberCount = w.WorkspaceMembers.Count,
                    MyRole = w.WorkspaceMembers.Any(wm => wm.UserId == userId && wm.RoleId == _ownerRoleId) ? "Owner" : "Member"
                }).FirstOrDefaultAsync();
            return workspace;
        }
        private async Task<bool> IsOwnerAsync(int workspaceId,int userId)
        {
            return await _context.WorkspaceMembers.AnyAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == userId &&wm.RoleId == _ownerRoleId);
        }
        public async Task<bool> UpdateWorkspaceAsync(int workspaceId, UpdateWorkspaceDTO updateWorkspaceDTO, int userId)
        {
            var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if(workspace == null)
            {
                return false;
            }
            bool isOwner = await IsOwnerAsync(workspaceId, userId);
            if (!isOwner)
            {
                return false;
            }
            workspace.Name = updateWorkspaceDTO.Name.Trim();
            workspace.Description = string.IsNullOrWhiteSpace(updateWorkspaceDTO.Description)? null: updateWorkspaceDTO.Description.Trim();
            workspace.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteWorkspaceAsync(int workspaceId, int userId)
        {
            var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if(workspace == null)
            {
                return false;
            }
            if (!await IsOwnerAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Chỉ owner được xóa workspace.");
            }
            bool hasBoards = await _context.Boards.AnyAsync(b => b.WorkspaceId == workspaceId);
            bool hasLabels = await _context.Labels.AnyAsync(l => l.WorkspaceId == workspaceId);
            bool hasLogs = await _context.ActivityLogs.AnyAsync(a => a.WorkspaceId == workspaceId);
            if (hasBoards || hasLabels || hasLogs)
            {
                throw new BusinessConflictException("Workspace còn board, nhãn hoặc nhật ký hoạt động.");
            }
            var members = await _context.WorkspaceMembers.Where(wm => wm.WorkspaceId == workspaceId).ToListAsync();
            _context.WorkspaceMembers.RemoveRange(members);
            _context.Workspaces.Remove(workspace);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> AddMemberAsync(int workspaceId, AddWorkspaceMemberDTO addWorkspaceDTO, int currentUserId)
        {
            bool workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId);
            if (!workspaceExists)
            {
                return false;
            }
            bool isOwner = await IsOwnerAsync(workspaceId,currentUserId);
            if (!isOwner)
            {
                return false;
            }
            bool userExists = await _context.Users.AnyAsync(u => u.UserId == addWorkspaceDTO.UserId);

            if (!userExists)
            {
                return false;
            }
            bool alreadyMember = await _context.WorkspaceMembers.AnyAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == addWorkspaceDTO.UserId);
            if (alreadyMember)
            {
                return false;
            }
            var member = new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = addWorkspaceDTO.UserId,
                RoleId = _memberRoleId,
                JoinedAt = DateTime.UtcNow
            };
            _context.WorkspaceMembers.Add(member);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<WorkspaceMemberResponseDTO>?> GetMembersAsync(int workspaceId, int currentUserId)
        {
            bool isMember = await IsMemberAsync(workspaceId,currentUserId);
            if (!isMember)
            {
                return null;
            }
            var members = await _context.WorkspaceMembers.AsNoTracking().Where(wm => wm.WorkspaceId == workspaceId).OrderBy(wm => wm.JoinedAt)
                .Select(wm => new WorkspaceMemberResponseDTO
                {
                    WorkspaceId = wm.WorkspaceId,
                    UserId = wm.UserId,
                    Username = wm.User.Username,
                    Email = wm.User.Email,
                    RoleId = wm.RoleId,
                    WorkspaceRole = wm.Role.Name,
                    JoinedAt = wm.JoinedAt
                }).ToListAsync();
            return members;
        }
        public async Task<bool> RemoveMemberAsync(int workspaceId, int memberUserId, int currentUserId)
        {
            bool isOwner = await IsOwnerAsync(workspaceId,currentUserId);
            if (!isOwner)
            {
                return false;
            }
            var member = await _context.WorkspaceMembers.FirstOrDefaultAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == memberUserId);
            if (member == null)
            {
                return false;
            }
            if (member.RoleId == _ownerRoleId)
            {
                return false;
            }
            _context.WorkspaceMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
