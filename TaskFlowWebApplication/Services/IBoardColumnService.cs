using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Services
{
    public interface IBoardColumnService
    {
        Task<ColumnResponseDTO> CreateColumnAsync(int boardId, CreateColumnDTO dto, int userId);
        Task<List<ColumnResponseDTO>> GetColumnsAsync(int boardId, int userId);
        Task<ColumnResponseDTO> UpdateColumnAsync(int columnId, UpdateColumnDTO dto, int userId);
        System.Threading.Tasks.Task DeleteColumnAsync(int columnId, int userId);
        System.Threading.Tasks.Task ReorderColumnsAsync(int boardId, ReorderColumnsDTO dto, int userId);
    }
}
    