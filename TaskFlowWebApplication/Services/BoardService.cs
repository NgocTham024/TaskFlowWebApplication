using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public class BoardService: IBoardService
    {
        private readonly AppDbContext _context;
        public BoardService(AppDbContext context)
        {
            _context = context;
        }
        private async Task<WorkspaceMember> GetMembershipAsync(int workspaceId,int userId)
        {
            var membership = await _context.WorkspaceMembers.Include(wm => wm.Role).AsNoTracking()
                .FirstOrDefaultAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == userId);
            if (membership == null)
            {
                bool workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("Không tìm thấy không gian làm việc.");
                }
                throw new UnauthorizedAccessException("Bạn không phải thành viên của không gian làm việc này.");
            }
            return membership;
        }
        private static void EnsureOwner(WorkspaceMember membership)
        {
            if (!string.Equals(membership.Role.Name,"owner",StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu không gian làm việc mới có thể thực hiện thao tác này.");
            }
        }
        private static BoardResponseDTO MapToResponse(Board board, string roleName)
        {
            return new BoardResponseDTO
            {
                BoardId = board.Id,
                WorkspaceId = board.WorkspaceId,
                Name = board.Name,
                Description = board.Description,
                CreatedAt = board.CreatedAt,
                UpdatedAt = board.UpdatedAt,
                MyRole = roleName
            };
        }
        public async Task<BoardResponseDTO> CreateBoardAsync(int workspaceId, CreateBoardDTO createBoardDTO, int userId)
        {
            var membership = await GetMembershipAsync(workspaceId, userId);
            EnsureOwner(membership);
            if (string.IsNullOrWhiteSpace(createBoardDTO.Name))
            {
                throw new ArgumentException("Tên bảng không được để trống.");

            }
            var now = DateTime.UtcNow;
            var board = new Board
            {
                WorkspaceId = workspaceId,
                Name = createBoardDTO.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(createBoardDTO.Description) ? null: createBoardDTO.Description.Trim(),
                CreatedBy = userId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Boards.Add(board);

            await _context.SaveChangesAsync();
            return MapToResponse(board, membership.Role.Name);
        }
        public async Task<List<BoardResponseDTO>> GetBoardsByWorkspaceAsync(int workspaceId, int userId)
        {
            var membership = await GetMembershipAsync(workspaceId, userId);
            var boards = await _context.Boards.AsNoTracking().Where(b => b.WorkspaceId == workspaceId).OrderByDescending(b => b.CreatedAt).ToListAsync();
            return boards.Select(b => MapToResponse(b, membership.Role.Name)).ToList();
        }
        public async Task<BoardResponseDTO?> GetBoardByIdAsync(int boardId,int userId)
        {
            var board = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bảng.");
            }
            var membership = await GetMembershipAsync(board.WorkspaceId,userId);
            return MapToResponse(board, membership.Role.Name);
        }
        public async Task<BoardResponseDTO> UpdateBoardAsync(int boardId,UpdateBoardDTO updateBoardDTO, int userId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bảng.");
            }
            var membership = await GetMembershipAsync(board.WorkspaceId,userId);
            EnsureOwner(membership);
            if (string.IsNullOrWhiteSpace(updateBoardDTO.Name))
            {
                throw new ArgumentException("Tên bảng không được để trống.");
            }

            board.Name = updateBoardDTO.Name.Trim();
            board.Description = string.IsNullOrWhiteSpace(updateBoardDTO.Description) ? null : updateBoardDTO.Description.Trim();
            board.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToResponse(board, membership.Role.Name);
        }
        public async System.Threading.Tasks.Task DeleteBoardAsync(int boardId, int userId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bảng.");
            }
            var membership = await GetMembershipAsync(board.WorkspaceId,userId);
            EnsureOwner(membership);
            bool hasColumns = await _context.BoardColumns.AnyAsync(c => c.BoardId == boardId);
            bool hasActivityLogs = await _context.ActivityLogs.AnyAsync(a => a.BoardId == boardId);
            bool hasNotifications = await _context.Notifications.AnyAsync(n => n.BoardId == boardId);
            if (hasColumns || hasActivityLogs || hasNotifications)
            {
                throw new InvalidOperationException("Không thể xóa bảng này vì bảng vẫn còn chứa các cột, nhật ký hoạt động hoặc thông báo.");
            }
            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();
        }
    }
}
