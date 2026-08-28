using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Web.Authorization;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Entities;
using SchoolManagementSystem.Web.ViewModels.Admin;

namespace SchoolManagementSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class AccountingController : Controller
{
    private readonly AppDbContext _context;

    public AccountingController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        int? year,
        int? month,
        int? groupId,
        string? status,
        string? search)
    {
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        if (!IsValidPeriod(selectedYear, selectedMonth))
        {
            return BadRequest();
        }

        var studentQuery = _context.Students
            .Include(s => s.Group)
            .AsQueryable();

        if (groupId.HasValue)
        {
            studentQuery = studentQuery.Where(s => s.GroupId == groupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            studentQuery = studentQuery.Where(s =>
                s.FirstName.ToLower().Contains(value) ||
                s.LastName.ToLower().Contains(value) ||
                (s.Group != null && s.Group.Name.ToLower().Contains(value)));
        }

        var students = await studentQuery
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();
        var payments = studentIds.Count == 0
            ? new List<StudentPayment>()
            : await _context.StudentPayments
                .Where(p =>
                    p.Year == selectedYear &&
                    p.Month == selectedMonth &&
                    studentIds.Contains(p.StudentId))
                .ToListAsync();

        var paymentsByStudent = payments.ToDictionary(p => p.StudentId);
        var allRows = new List<StudentAccountingRowViewModel>();

        foreach (var student in students)
        {
            paymentsByStudent.TryGetValue(student.Id, out var payment);

            var expected = payment?.ExpectedAmount ?? 0m;
            var paid = payment?.PaidAmount ?? 0m;
            var rowStatus = ResolveStatus(expected, paid);

            allRows.Add(new StudentAccountingRowViewModel
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                GroupName = student.Group?.Name,
                ExpectedAmount = expected,
                PaidAmount = paid,
                Balance = Math.Max(expected - paid, 0m),
                PaidAt = payment?.PaidAt,
                Note = payment?.Note,
                Status = rowStatus
            });
        }

        var normalizedStatus = NormalizeStatus(status);
        var visibleRows = normalizedStatus == null
            ? allRows
            : allRows.Where(r => r.Status == normalizedStatus).ToList();

        visibleRows = visibleRows
            .OrderBy(r => StatusOrder(r.Status))
            .ThenBy(r => r.StudentName)
            .ToList();

        var model = new StudentAccountingViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            GroupId = groupId,
            Status = normalizedStatus,
            Search = search,
            Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync(),
            Rows = visibleRows,
            TotalExpected = allRows.Sum(r => r.ExpectedAmount),
            TotalPaid = allRows.Sum(r => r.PaidAmount),
            TotalDebt = allRows.Sum(r => r.Balance),
            PaidCount = allRows.Count(r => r.Status == "paid"),
            PartialCount = allRows.Count(r => r.Status == "partial"),
            UnpaidCount = allRows.Count(r => r.Status == "unpaid"),
            NotSetCount = allRows.Count(r => r.Status == "not-set")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int studentId,
        int year,
        int month,
        decimal expectedAmount,
        decimal paidAmount,
        DateTime? paidAt,
        string? note,
        int? groupId,
        string? status,
        string? search)
    {
        if (!IsValidPeriod(year, month) || expectedAmount < 0 || paidAmount < 0)
        {
            TempData["Error"] = "Проверьте период и суммы. Значения не могут быть отрицательными.";
            return RedirectToIndex(year, month, groupId, status, search);
        }

        var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
        if (!studentExists)
        {
            return NotFound();
        }

        var payment = await _context.StudentPayments.FirstOrDefaultAsync(p =>
            p.StudentId == studentId && p.Year == year && p.Month == month);

        if (payment == null)
        {
            payment = new StudentPayment
            {
                StudentId = studentId,
                Year = year,
                Month = month,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentPayments.Add(payment);
        }

        payment.ExpectedAmount = expectedAmount;
        payment.PaidAmount = paidAmount;
        payment.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        payment.UpdatedAt = DateTime.UtcNow;

        if (paidAmount <= 0)
        {
            payment.PaidAt = null;
        }
        else if (paidAt.HasValue)
        {
            payment.PaidAt = DateTime.SpecifyKind(paidAt.Value.Date, DateTimeKind.Utc);
        }
        else
        {
            payment.PaidAt ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Оплата ученика сохранена.";
        return RedirectToIndex(year, month, groupId, status, search);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(
        int studentId,
        int year,
        int month,
        int? groupId,
        string? status,
        string? search)
    {
        if (!IsValidPeriod(year, month))
        {
            return BadRequest();
        }

        var payment = await _context.StudentPayments.FirstOrDefaultAsync(p =>
            p.StudentId == studentId && p.Year == year && p.Month == month);

        if (payment == null || payment.ExpectedAmount <= 0)
        {
            TempData["Error"] = "Сначала установите сумму к оплате для этого ученика.";
            return RedirectToIndex(year, month, groupId, status, search);
        }

        payment.PaidAmount = payment.ExpectedAmount;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ученик отмечен как полностью оплативший.";
        return RedirectToIndex(year, month, groupId, status, search);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMonthlyFee(
        int year,
        int month,
        decimal amount,
        int? groupId)
    {
        if (!IsValidPeriod(year, month) || amount < 0)
        {
            TempData["Error"] = "Некорректный период или сумма.";
            return RedirectToIndex(year, month, groupId, null, null);
        }

        var studentQuery = _context.Students.AsQueryable();
        if (groupId.HasValue)
        {
            studentQuery = studentQuery.Where(s => s.GroupId == groupId.Value);
        }

        var studentIds = await studentQuery.Select(s => s.Id).ToListAsync();
        if (studentIds.Count == 0)
        {
            TempData["Error"] = "Для выбранного фильтра ученики не найдены.";
            return RedirectToIndex(year, month, groupId, null, null);
        }

        var existing = await _context.StudentPayments
            .Where(p =>
                p.Year == year &&
                p.Month == month &&
                studentIds.Contains(p.StudentId))
            .ToDictionaryAsync(p => p.StudentId);

        foreach (var studentId in studentIds)
        {
            if (!existing.TryGetValue(studentId, out var payment))
            {
                payment = new StudentPayment
                {
                    StudentId = studentId,
                    Year = year,
                    Month = month,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StudentPayments.Add(payment);
            }

            payment.ExpectedAmount = amount;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = groupId.HasValue
            ? "Месячная сумма установлена для выбранной группы."
            : "Месячная сумма установлена для всех учеников.";

        return RedirectToIndex(year, month, groupId, null, null);
    }

    private IActionResult RedirectToIndex(
        int year,
        int month,
        int? groupId,
        string? status,
        string? search)
    {
        return RedirectToAction(nameof(Index), new
        {
            year,
            month,
            groupId,
            status,
            search
        });
    }

    private static bool IsValidPeriod(int year, int month)
    {
        return year is >= 2000 and <= 2100 && month is >= 1 and <= 12;
    }

    private static string ResolveStatus(decimal expected, decimal paid)
    {
        if (expected <= 0)
        {
            return "not-set";
        }

        if (paid <= 0)
        {
            return "unpaid";
        }

        return paid < expected ? "partial" : "paid";
    }

    private static string? NormalizeStatus(string? status)
    {
        var value = status?.Trim().ToLowerInvariant();
        return value is "paid" or "partial" or "unpaid" or "not-set"
            ? value
            : null;
    }

    private static int StatusOrder(string status)
    {
        return status switch
        {
            "unpaid" => 0,
            "partial" => 1,
            "not-set" => 2,
            "paid" => 3,
            _ => 4
        };
    }
}
