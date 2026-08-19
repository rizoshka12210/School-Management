using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.ViewModels.Shared;

public class CalendarGridViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public bool CanManage { get; set; }

    public List<CalendarEvent> Events { get; set; } = new();
}
