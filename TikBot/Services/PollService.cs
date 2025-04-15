using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TikBot.Models;

namespace TikBot.Services
{
    public class PollService
    {
        private readonly List<PollResponse> _pollResponses = new List<PollResponse>();
        private readonly StudentService _studentService;
        private readonly string _pollStoragePath = @"C:\TikBotData\PollResponses.json";

        public PollService(StudentService studentService)
        {
            _studentService = studentService;
            SavePollResponsesAsync().GetAwaiter().GetResult();
        }

        public bool CanUseReportReviewed(long userId, long groupId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var lastResponse = _pollResponses
                .Where(r => r.StudentId == userId && r.GroupId == groupId && r.Type == PollType.ReportReviewed)
                .OrderByDescending(r => r.RespondedAt)
                .FirstOrDefault();

            if (lastResponse != null && (DateTime.UtcNow - lastResponse.RespondedAt) < TimeSpan.FromHours(1))
            {
                errorMessage = "شما تا ۱ ساعت دیگر نمی‌توانید از این دکمه استفاده کنید.";
                return false;
            }

            return true;
        }

        public bool CanUseReportNotSent(long userId, long groupId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var lastResponse = _pollResponses
                .Where(r => r.StudentId == userId && r.GroupId == groupId && r.Type == PollType.ReportNotSent)
                .OrderByDescending(r => r.RespondedAt)
                .FirstOrDefault();

            if (lastResponse != null && (DateTime.UtcNow - lastResponse.RespondedAt) < TimeSpan.FromHours(3))
            {
                errorMessage = "شما تا ۳ ساعت دیگر نمی‌توانید از این دکمه استفاده کنید.";
                return false;
            }

            return true;
        }

        public void RecordPollResponse(long userId, long groupId, PollType type)
        {
            _pollResponses.Add(new PollResponse
            {
                StudentId = userId,
                GroupId = groupId,
                RespondedAt = DateTime.UtcNow,
                Type = type
            });
            SavePollResponsesAsync().GetAwaiter().GetResult();
        }

        public void RecordPollScore(long userId, long groupId, int score)
        {
            _pollResponses.Add(new PollResponse
            {
                StudentId = userId,
                GroupId = groupId,
                RespondedAt = DateTime.UtcNow,
                Type = PollType.ReportReviewed,
                Score = score
            });
            SavePollResponsesAsync().GetAwaiter().GetResult();
        }

        public async Task SavePollResponsesAsync()
        {
            var json = JsonSerializer.Serialize(_pollResponses, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_pollStoragePath)!);
            await File.WriteAllTextAsync(_pollStoragePath, json);
        }

        public List<PollResponse> GetReviewedReports(long mentorId, DateTime startTime, DateTime endTime)
        {
            return _pollResponses
                .Where(r => r.Type == PollType.ReportReviewed && _studentService.GetAllStudents().Any(s => s.UserId == r.StudentId && s.MentorId == mentorId)
                            && r.RespondedAt >= startTime && r.RespondedAt <= endTime)
                .ToList();
        }

        public List<PollResponse> GetNotSentReports(long mentorId, DateTime startTime, DateTime endTime)
        {
            return _pollResponses
                .Where(r => r.Type == PollType.ReportNotSent && _studentService.GetAllStudents().Any(s => s.UserId == r.StudentId && s.MentorId == mentorId)
                            && r.RespondedAt >= startTime && r.RespondedAt <= endTime)
                .ToList();
        }

        public double GetAveragePollScore(long mentorId, long groupId, DateTime startTime, DateTime endTime)
        {
            var scores = _pollResponses
                .Where(r => r.Type == PollType.ReportReviewed && _studentService.GetAllStudents().Any(s => s.UserId == r.StudentId && s.MentorId == mentorId)
                            && r.GroupId == groupId && r.RespondedAt >= startTime && r.RespondedAt <= endTime && r.Score.HasValue)
                .Select(r => r.Score.Value)
                .ToList();

            return scores.Any() ? Math.Round(scores.Average(), 2) : 0;
        }
    }
}
