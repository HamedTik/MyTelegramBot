using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Models;

namespace TikBot.Services
{
    public class ReportService
    {
        private readonly List<Report> _reports = new List<Report>();
        private readonly List<(long MentorId, long GroupId, int MessageId)> _pendingReports = new List<(long, long, int)>();
        private readonly UserService _userService;
        private readonly string _reportsStoragePath = @"C:\TikBotData\Reports.json";

        public ReportService(UserService userService)
        {
            _userService = userService;
            SaveReportsAsync().GetAwaiter().GetResult();
        }

        public void StartPendingReport(long mentorId, long groupId, int messageId)
        {
            _pendingReports.Add((mentorId, groupId, messageId));
        }

        public bool IsPendingReport(long mentorId, long groupId, int messageId)
        {
            return _pendingReports.Any(p => p.MentorId == mentorId && p.GroupId == groupId && p.MessageId == messageId);
        }

        public int GetPendingMessageId(long mentorId, long groupId)
        {
            var pending = _pendingReports.FirstOrDefault(p => p.MentorId == mentorId && p.GroupId == groupId);
            return pending.MessageId;
        }

        public void CompletePendingReport(long mentorId, long groupId, int messageId)
        {
            var pending = _pendingReports.FirstOrDefault(p => p.MentorId == mentorId && p.GroupId == groupId && p.MessageId == messageId);
            if (pending != default)
            {
                _pendingReports.Remove(pending);
            }
        }

        public void RecordReport(long mentorId, long groupId, string reason)
        {
            _reports.Add(new Report
            {
                MentorId = mentorId,
                GroupId = groupId,
                Reason = reason,
                ReportedAt = DateTime.UtcNow
            });
            SaveReportsAsync().GetAwaiter().GetResult();
        }

        public async Task SaveReportsAsync()
        {
            var json = JsonSerializer.Serialize(_reports, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_reportsStoragePath)!);
            await File.WriteAllTextAsync(_reportsStoragePath, json);
        }

        public async Task<(string GroupName, string GroupLink)> GetGroupInfoAsync(ITelegramBotClient botClient, long groupId, CancellationToken cancellationToken)
        {
            try
            {
                var chat = await botClient.GetChatAsync(groupId, cancellationToken);
                var groupName = chat.Title ?? "گروه بدون نام";
                var groupLink = chat.InviteLink ?? await botClient.ExportChatInviteLinkAsync(groupId, cancellationToken);
                return (groupName, groupLink);
            }
            catch
            {
                return ("گروه بدون نام", "لینک نامشخص");
            }
        }

        public string GetMentorName(long mentorId)
        {
            var mentor = _userService.GetMentor(mentorId);
            return mentor?.FullName ?? "منتور ناشناس";
        }

        public string GetMentorLink(long mentorId)
        {
            return $"tg://user?id={mentorId}";
        }
    }
}
