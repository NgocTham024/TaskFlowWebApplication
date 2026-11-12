using System;
using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
	public class UpdateTaskScheduleDTO
	{
		public DateTime? StartDate { get; set; }

		public DateTime? DueDate { get; set; }
	}
}