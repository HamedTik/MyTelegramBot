using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using TikBot.Models;

namespace TikBot.Services
{
    public class PerformanceMonitorService
    {
        private readonly UserService _userService;
        private readonly GroupService _groupService;
        private readonly StudentService _studentService;
        private readonly PollService _pollService;
        private readonly MentorService _mentorService;
        private readonly ITelegramBotClient _botClient;
        private readonly string _storagePath = @"C:\TikBotData\PerformanceReports.json";

        public PerformanceMonitorService(
            UserService userService,
            GroupService groupService,
            StudentService studentService,
            PollService pollService,
            ITelegramBotClient botClient)
        {
            _userService = userService;
            _groupService = groupService;
            _studentService = studentService;
            _pollService = pollService;
            _botClient = botClient;
        }

        public async Task CollectReportsAsync(CancellationToken cancellationToken)
        {
            var mentors = _userService.GetAllMentors(); // تغییر به GetAllMentors
            var reports = new List<MentorPerformanceReport>();
            var startTime = DateTime.UtcNow.AddHours(3.5).Date.AddDays(-1).AddHours(7); // Yesterday 7 AM Iran
            var endTime = DateTime.UtcNow.AddHours(3.5).Date.AddHours(7); // Today 7 AM Iran

            foreach (var mentor in mentors)
            {
                var report = new MentorPerformanceReport
                {
                    MentorId = mentor.ChatId,
                    Date = DateTime.UtcNow
                };

                // Get progress groups for the mentor
                var groups = _studentService.GetAllStudents() // تغییر به GetAllStudents
                    .Where(s => s.MentorId == mentor.ChatId && _groupService.GetGroupType(s.GroupId) == GroupType.Progress)
                    .Select(s => s.GroupId)
                    .Distinct()
                    .ToList();

                report.TotalGroups = groups.Count(); // استفاده از Count() به جای Count

                // Reviewed groups (Y): Based on "ReportReviewed" button
                var reviewedGroups = _pollService.GetReviewedReports(mentor.ChatId, startTime, endTime)
                    .Select(r => r.GroupId)
                    .Distinct()
                    .ToList();
                report.ReviewedGroups = reviewedGroups.Count();

                // Average review score: Based on scores from "ReportReviewed" polls
                var totalScore = 0.0;
                var scoreCount = 0;
                foreach (var groupId in reviewedGroups)
                {
                    var avgScore = _pollService.GetAveragePollScore(mentor.ChatId, groupId, startTime, endTime);
                    if (avgScore > 0)
                    {
                        totalScore += avgScore;
                        scoreCount++;
                    }
                }
                report.AverageReviewScore = scoreCount > 0 ? Math.Round(totalScore / scoreCount, 2) : 0;

                // Not sent groups (Z): Based on "ReportNotSent" button
                var notSentGroups = _pollService.GetNotSentReports(mentor.ChatId, startTime, endTime)
                    .Select(r => r.GroupId)
                    .Distinct()
                    .ToList();
                report.NotSentGroups = notSentGroups.Count();
                report.NotSentGroupLinks = await GetGroupLinksAsync(notSentGroups, cancellationToken);

                // Inactive groups (Q): Groups with neither "ReportReviewed" nor "ReportNotSent"
                var inactiveGroups = groups.Except(reviewedGroups).Except(notSentGroups).ToList();
                report.InactiveGroups = inactiveGroups.Count(); // استفاده از Count() به جای Count

                reports.Add(report);
            }

            // Save reports to JSON file
            var json = JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
            await File.WriteAllTextAsync(_storagePath, json, cancellationToken);
        }

        public async Task SendReportsAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_storagePath)) return;

            var json = await File.ReadAllTextAsync(_storagePath, cancellationToken);
            var reports = JsonSerializer.Deserialize<List<MentorPerformanceReport>>(json);

            foreach (var report in reports!)
            {
                var mentor = _userService.GetMentor(report.MentorId);
                if (mentor == null) continue;

                // بررسی مرخصی
                if (_mentorService.IsOnLeaveTomorrow(report.MentorId))
                {
                    continue; // گزارش برای منتور در مرخصی ارسال نمی‌شود
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"سلام منتور عزیز، صبح‌بخیر");
                messageBuilder.AppendLine($"از تعداد {report.TotalGroups} دانش‌آموز فعال شما روز گذشته:");

                if (report.ReviewedGroups > 0)
                {
                    messageBuilder.AppendLine($"{report.ReviewedGroups} دانش‌آموز با نمره {report.AverageReviewScore} گزارش بررسی کردی");
                }

                if (report.NotSentGroups > 0)
                {
                    messageBuilder.AppendLine($"{report.NotSentGroups} دانش‌آموز گزارش ارسال نکردند");
                    messageBuilder.AppendLine(string.Join("\n", report.NotSentGroupLinks.Select(g => $"[{g.GroupName}]({g.GroupLink})")));
                }

                if (report.InactiveGroups > 0)
                {
                    messageBuilder.AppendLine($"\n{report.InactiveGroups} دانش‌آموز هیچ بررسی انجام ندادی");
                    messageBuilder.AppendLine(string.Join("\n", report.InactiveGroupLinks.Select(g => $"[{g.GroupName}]({g.GroupLink})")));
                }

                await _botClient.SendTextMessageAsync(
                    chatId: mentor.UpgradeGroupId,
                    text: messageBuilder.ToString().Trim(),
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task<List<GroupInfo>> GetGroupLinksAsync(List<long> groupIds, CancellationToken cancellationToken)
        {
            var links = new List<GroupInfo>();
            foreach (var groupId in groupIds)
            {
                try
                {
                    var chat = await _botClient.GetChatAsync(groupId, cancellationToken);
                    var groupName = chat.Title ?? "گروه بدون نام";
                    var groupLink = chat.InviteLink ?? await _botClient.ExportChatInviteLinkAsync(groupId, cancellationToken);
                    links.Add(new GroupInfo
                    {
                        GroupId = groupId,
                        GroupName = groupName,
                        GroupLink = groupLink
                    });
                }
                catch
                {
                    links.Add(new GroupInfo
                    {
                        GroupId = groupId,
                        GroupName = "گروه بدون نام",
                        GroupLink = "لینک نامشخص"
                    });
                }
            }
            return links;
        }
    }
}
