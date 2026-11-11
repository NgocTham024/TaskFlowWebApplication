using System.ComponentModel.DataAnnotations;
namespace TaskFlowWebApplication.DTOs
{
    public class WorkspaceMemberResponseDTO
    {
        public int UserId { get; set; }
        public int WorkspaceId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WorkspaceRole { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
