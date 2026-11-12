using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Services
{
    public interface IWorkspaceService
    {
        Task<WorkspaceResponseDTO> CreateWorkspaceAsync(CreateWorkspaceDTO createWorkspaceDTO, int userId);
        Task<List<WorkspaceResponseDTO>> GetWorkspacesAsync(int userId);
        Task<WorkspaceResponseDTO?> GetWorkspacesByIdAsync(int workspaceId,int userId);
        Task<bool> UpdateWorkspaceAsync(int workspaceId, UpdateWorkspaceDTO updateSpaceDTO, int userId);
        Task<bool> DeleteWorkspaceAsync(int workspaceId, int userId);
        Task<bool> AddMemberAsync(int workspaceId, AddWorkspaceMemberDTO addWorkspaceDTO, int currentUserId);
        Task<List<WorkspaceMemberResponseDTO>?> GetMembersAsync(int workspaceId, int currentUserId);
        Task<bool> RemoveMemberAsync(int workspaceId, int memberUserId, int currentUserId);
    }
}
