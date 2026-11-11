using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class TaskFilterDTO
    {
        [Range(1, int.MaxValue)]
        public int? StatusId { get; set; }

        [Range(1, int.MaxValue)]
        public int? AssigneeId { get; set; }
        // các mốc thời gian theo utc.
        public DateTimeOffset? DueFrom { get; set; }
        // mốc kết thúc không bao gồm.
        public DateTimeOffset? DueBefore { get; set; }
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
        [StringLength(200)]
        public string? Search { get; set; }

        [Range(1, int.MaxValue)]
        public int? PriorityId { get; set; }
    }
}
