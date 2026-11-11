using System.ComponentModel.DataAnnotations;
namespace TaskFlowWebApplication.DTOs
{
    public class WorkspaceResponseDTO
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CreateBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int MemberCount { get; set; }
        public string? MyRole { get; set; }
        public bool IsOwner { get; set; }
    }
}
