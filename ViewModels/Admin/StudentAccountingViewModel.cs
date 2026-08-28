using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.ViewModels.Admin;

public class StudentAccountingViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? GroupId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }

    public List<Group> Groups { get; set; } = new();
    public List<StudentAccountingRowViewModel> Rows { get; set; } = new();

    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDebt { get; set; }

    public int PaidCount { get; set; }
    public int PartialCount { get; set; }
    public int UnpaidCount { get; set; }
    public int NotSetCount { get; set; }
}

public class StudentAccountingRowViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? GroupName { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }

    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }

    public string Status { get; set; } = "not-set";
}
