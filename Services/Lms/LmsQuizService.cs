namespace HRM.Services.Lms;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Multiple-choice quiz grading — pure server-side auto-grade against
// Lms_QuizQuestion.CorrectChoice. Each attempt is recorded in full
// (Lms_QuizAttempt + Lms_QuizAnswer per question) so a failed attempt is
// never silently discarded — the employee can retake, and every attempt
// stays in the history.
public class LmsQuizService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<long> SubmitAttemptAsync(long enrollmentId, Dictionary<long, string> answers, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบการลงทะเบียนนี้แล้ว");
        var session = await context.Lms_CourseSessions.FirstOrDefaultAsync(s => s.Id == enrollment.CourseSessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบอบรมของการลงทะเบียนนี้");
        var course = await context.Lms_Courses.FirstOrDefaultAsync(c => c.Id == session.CourseId, ct)
            ?? throw new InvalidOperationException("ไม่พบหลักสูตรนี้แล้ว");

        var questions = await context.Lms_QuizQuestions.Where(q => q.CourseId == course.Id).ToListAsync(ct);
        if (questions.Count == 0)
            throw new InvalidOperationException("หลักสูตรนี้ยังไม่มีแบบทดสอบ");

        var quizAnswers = new List<Lms_QuizAnswer>();
        var correctCount = 0;
        foreach (var question in questions)
        {
            var selected = answers.TryGetValue(question.Id, out var choice) ? choice : "";
            var isCorrect = string.Equals(selected, question.CorrectChoice, StringComparison.OrdinalIgnoreCase);
            if (isCorrect) correctCount++;

            quizAnswers.Add(new Lms_QuizAnswer
            {
                QuestionId = question.Id,
                SelectedChoice = string.IsNullOrEmpty(selected) ? "-" : selected,
                IsCorrect = isCorrect,
            });
        }

        var scorePercent = Math.Round(100m * correctCount / questions.Count, 2);
        var isPassed = course.PassingScorePercent is int passingScore && scorePercent >= passingScore;

        var attempt = new Lms_QuizAttempt
        {
            EnrollmentId = enrollmentId,
            ScorePercent = scorePercent,
            IsPassed = isPassed,
        };
        context.Lms_QuizAttempts.Add(attempt);
        await context.SaveChangesAsync(ct);

        foreach (var answer in quizAnswers)
            answer.QuizAttemptId = attempt.Id;
        context.Lms_QuizAnswers.AddRange(quizAnswers);

        enrollment.QuizScorePercent = scorePercent;
        await context.SaveChangesAsync(ct);

        return attempt.Id;
    }
}
