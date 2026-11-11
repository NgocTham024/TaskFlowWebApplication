using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public class BoardColumnService : IBoardColumnService
    {
        private readonly AppDbContext _context;
        public BoardColumnService(AppDbContext context)
        {
            _context = context;
        }
        private async Task<WorkspaceMember> GetMembershipAsync(int workspaceId, int userId)
        {
            var membership = await _context.WorkspaceMembers.Include(wm => wm.Role).AsNoTracking().FirstOrDefaultAsync(wm =>
            wm.WorkspaceId == workspaceId &&wm.UserId == userId);
            if (membership == null)
            {
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của không gian làm việc.");
            }
            return membership;
        }
        private static void EnsureOwner(WorkspaceMember membership)
        {
            if (!string.Equals(membership.Role.Name,"owner",StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu mói có thể quản lý các cột.");
            }
        }
        private async Task<Board>GetBoardAsync(int boardId)
        {
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bảng.");
            }
            return board;
        }
        private async Task<BoardColumn>GetColumnAsync(int columnId)
        {
            var column = await _context.BoardColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == columnId);
            if (column == null)
            {
                throw new KeyNotFoundException("Column not found.");
            }
            return column;
        }
        private static ColumnResponseDTO MapToResponse(BoardColumn column)
        {
            return new ColumnResponseDTO
            {
                ColumnId = column.Id,
                BoardId = column.BoardId,
                Name = column.Name,
                Position = column.Position,
                CreatedAt = column.CreatedAt,
                UpdatedAt = column.UpdatedAt
            };
        }
        public async Task<ColumnResponseDTO> CreateColumnAsync(int boardId, CreateColumnDTO createColumnDTO, int userId)
        {
            var board = await GetBoardAsync(boardId);
            var membership = await GetMembershipAsync(board.WorkspaceId, userId);
            EnsureOwner(membership);
            if (string.IsNullOrWhiteSpace(createColumnDTO.Name))
            {
                throw new ArgumentException("Tên cột không được để trống.");
            }
            int? maxPosition = await _context.BoardColumns.Where(c => c.BoardId == boardId).Select(c => (int?)c.Position).MaxAsync();
            var now = DateTime.UtcNow;
            var trimmedName = createColumnDTO.Name.Trim();
            var column = new BoardColumn
            {
                BoardId = boardId,
                Name = createColumnDTO.Name.Trim(),
                Position = (maxPosition ?? -1) + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TaskLists = new List<TaskList>
                {
                    new TaskList
                    {
                        Name = trimmedName,
                        Position = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                }
            };
            _context.BoardColumns.Add(column);
            await _context.SaveChangesAsync();
            return MapToResponse(column);
        }
        public async Task<List<ColumnResponseDTO>> GetColumnsAsync(int boardId, int userId)
        {
            var board = await GetBoardAsync(boardId);
            await GetMembershipAsync(board.WorkspaceId, userId);
            var columns = await _context.BoardColumns.Where(c => c.BoardId == boardId).OrderBy(c => c.Position).ThenBy(c => c.Id).ToListAsync();
            return columns.Select(MapToResponse).ToList();
        }
        public async Task<ColumnResponseDTO> UpdateColumnAsync(int columnId, UpdateColumnDTO updateColumnDTO, int userId)
        {
            var column = await GetColumnAsync(columnId);
            var membership = await GetMembershipAsync(column.Board.WorkspaceId, userId);
            EnsureOwner(membership);
            if (string.IsNullOrWhiteSpace(updateColumnDTO.Name))
            {
                throw new ArgumentException("Tên cột không được để trống.");
            } 
            column.Name = updateColumnDTO.Name.Trim();
            column.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToResponse(column);
        }
        public async System.Threading.Tasks.Task DeleteColumnAsync(int columnId, int userId)
        {
            var column = await GetColumnAsync(columnId);
            var membership = await GetMembershipAsync(column.Board.WorkspaceId, userId);
            EnsureOwner(membership);
            bool hasTasks = await _context.Tasks.AnyAsync(t => t.List.ColumnId == columnId);
            if (hasTasks)
            {
                throw new BusinessConflictException("Không thể xóa cột vì vẫn còn công việc bên trong. Hãy di chuyển hoặc xóa các công việc trước.");
            }
            // Xóa các danh sách rỗng thuộc cột trước khi xóa cột
            var emptyLists = await _context.TaskLists.Where(l => l.ColumnId == columnId).ToListAsync();
            if (emptyLists.Count > 0)
            {
                _context.TaskLists.RemoveRange(emptyLists);
            }
            _context.BoardColumns.Remove(column);
            await _context.SaveChangesAsync();
        }
        public async System.Threading.Tasks.Task ReorderColumnsAsync(int boardId, ReorderColumnsDTO reorderColumnsDTO, int userId)
        {
            var board = await GetBoardAsync(boardId);
            var membership = await GetMembershipAsync(board.WorkspaceId, userId);
            EnsureOwner(membership);

            if (reorderColumnsDTO.ColumnIds == null)
            {
                throw new ArgumentException("Bắt buộc phải có ID của cột.");
            }
            if (reorderColumnsDTO.ColumnIds.Distinct().Count() != reorderColumnsDTO.ColumnIds.Count)
            {
                throw new ArgumentException("ID bị trùng! ");
            }
            var columns = await _context.BoardColumns.Where(c => c.BoardId == boardId).ToListAsync();
            if (columns.Count != reorderColumnsDTO.ColumnIds.Count ||!columns.Select(c => c.Id).ToHashSet().SetEquals(reorderColumnsDTO.ColumnIds))
            {
                throw new ArgumentException("Các ID cột phải bao gồm tất cả các cột trong bảng này.");
            }
            if (columns.Count == 0)
            {
                return;
            }
            await using var transaction =await _context.Database.BeginTransactionAsync();
            int offset = columns.Max(c => c.Position) + columns.Count + 1;

            foreach (var column in columns)
            {
                column.Position += offset;
            }
            await _context.SaveChangesAsync();
            var columnDictionary = columns.ToDictionary(c => c.Id);
            for (int i = 0; i < reorderColumnsDTO.ColumnIds.Count; i++)
            {
                columnDictionary[reorderColumnsDTO.ColumnIds[i]].Position = i;
                columnDictionary[reorderColumnsDTO.ColumnIds[i]].UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
