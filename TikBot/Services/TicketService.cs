using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TikBot.Models;

namespace TikBot.Services
{
    public class TicketService
    {
        private readonly List<Ticket> _tickets = new List<Ticket>();
        private readonly List<(long StudentId, long GroupId, DateTime ReportReviewedAt)> _reportReviewedTimes = new List<(long, long, DateTime)>();
        private readonly StudentService _studentService;
        private readonly string _storagePath = @"C:\TikBotData\Tickets_Invites.json";

        public TicketService(StudentService studentService)
        {
            _studentService = studentService;
            SaveTicketsAsync().GetAwaiter().GetResult();
        }

        public bool CanIssueTicket(long studentId, long groupId, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_studentService.IsStudent(studentId, groupId))
            {
                errorMessage = "فقط دانش‌آموزان می‌توانند تیکت دریافت کنند.";
                return false;
            }

            var lastReport = _reportReviewedTimes.FirstOrDefault(r => r.StudentId == studentId && r.GroupId == groupId);

            if (lastReport == default)
            {
                errorMessage = "ابتدا باید گزارش شما بررسی شود.";
                return false;
            }

            var timeSinceReport = DateTime.UtcNow - lastReport.ReportReviewedAt;
            if (timeSinceReport > TimeSpan.FromHours(2))
            {
                errorMessage = "مهلت ۲ ساعته برای دریافت تیکت گذشته است.";
                return false;
            }

            var ticketsInWindow = _tickets.Count(t => t.StudentId == studentId && t.GroupId == groupId && t.IssuedAt >= lastReport.ReportReviewedAt);
            if (ticketsInWindow > 0)
            {
                errorMessage = "شما در این بازه یک تیکت دریافت کرده‌اید.";
                return false;
            }

            return true;
        }

        public void IssueTicket(long studentId, long groupId)
        {
            _tickets.Add(new Ticket
            {
                StudentId = studentId,
                GroupId = groupId,
                IssuedAt = DateTime.UtcNow
            });
            SaveTicketsAsync().GetAwaiter().GetResult();
        }

        public async Task SaveTicketsAsync()
        {
            var data = new { Tickets = _tickets };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
            await File.WriteAllTextAsync(_storagePath, json);
        }

        public int GetTicketCount(long studentId, long groupId)
        {
            return _tickets.Count(t => t.StudentId == studentId && t.GroupId == groupId);
        }

        public void RecordReportReviewed(long studentId, long groupId)
        {
            if (_studentService.IsStudent(studentId, groupId))
            {
                _reportReviewedTimes.Add((studentId, groupId, DateTime.UtcNow));
            }
        }

        public Student GetRandomStudent(long groupId)
        {
            return _studentService.GetRandomStudent(groupId);
        }
    }
}
