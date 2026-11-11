using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Services
{
    public interface IBoardService
    {
        Task<BoardResponseDTO> CreateBoardAsync(int workspaceId,CreateBoardDTO createBoardDTO, int userId);
        Task<List<BoardResponseDTO>> GetBoardsByWorkspaceAsync(int workspaceId,int userId);
        Task<BoardResponseDTO?> GetBoardByIdAsync(int boardId,int userId);
        Task<BoardResponseDTO> UpdateBoardAsync(int boardId,UpdateBoardDTO updateBoardDTO,int userId);
        Task DeleteBoardAsync(int boardId,int userId);
    }
}
