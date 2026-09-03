using SchoolManagementSystem.Web.Models.Entities;

namespace SchoolManagementSystem.Web.Services;

/// <summary>
/// Combines regular per-lesson grades with per-subject exam averages into
/// one "average grade" figure used across profiles, dashboards, the
/// leaderboard and risk/achievement checks. Each exam sheet's average
/// (Exam 1 + Exam 2) counts as a single extra data point per subject,
/// alongside every individual lesson grade, so entering exam results
/// updates a student's average the same way a lesson grade always has.
/// </summary>
public static class GradeAveragingHelper
{
    public static decimal? Combine(
        IEnumerable<decimal> lessonGrades,
        IEnumerable<decimal?> examAverages)
    {
        var values = new List<decimal>(lessonGrades);

        foreach (var examAverage in examAverages)
        {
            if (examAverage.HasValue)
            {
                values.Add(examAverage.Value);
            }
        }

        return values.Count == 0 ? null : Math.Round(values.Average(), 2);
    }

    /// <summary>
    /// ExamGrade is append-only (every change to a student's exam scores
    /// keeps the previous row instead of overwriting it, so nothing is
    /// ever lost), so any place that needs the *current* exam result -
    /// averages, achievements, the leaderboard - must first collapse the
    /// history down to the single latest row per student/subject.
    /// </summary>
    public static List<ExamGrade> LatestPerStudentSubject(IEnumerable<ExamGrade> examGrades)
    {
        return examGrades
            .GroupBy(e => new { e.StudentId, e.SubjectId })
            .Select(g => g
                .OrderByDescending(e => e.UpdatedAt)
                .ThenByDescending(e => e.Id)
                .First())
            .ToList();
    }
}
