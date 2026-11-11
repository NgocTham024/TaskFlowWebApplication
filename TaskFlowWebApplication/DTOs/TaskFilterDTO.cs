using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class TaskFilterDTO
    {
        [Range(1, int.MaxValue)]
        public int? StatusId { get; set; }

        [Range(1, int.MaxValue)]
        public int? AssigneeId { get; set; }
        // Các mốc thời gian theo UTC.
        public DateTimeOffset? DueFrom { get; set; }
        // Mốc kết thúc không bao gồm.
        public DateTimeOffset? DueBefore { get; set; }
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
