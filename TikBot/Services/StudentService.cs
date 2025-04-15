using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Handlers.UpgradeGroup;
using TikBot.Models;
using Message = Telegram.Bot.Types.Message;


namespace TikBot.Services
{
    public class StudentService
    {
        private readonly List<Student> _students = new List<Student>();
        private readonly UserService _userService;
        private readonly MentorService _mentorService;
        private readonly List<(long UserId, string Action, int MessageId)> _pendingActions = new List<(long, string, int)>();
        private readonly string _studentsStoragePath = @"C:\TikBotData\Students.json";

        public StudentService(UserService userService, MentorService mentorService)
        {
            _userService = userService;
            _mentorService = mentorService;
            SaveStudentsAsync().GetAwaiter().GetResult();
        }

        public async Task RegisterStudentAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var groupId = message.Chat.Id;
            var userRole = _userService.GetUserRole(userId, groupId);

            if (userRole != UserRole.Student) return;

            var mentorId = _userService.GetMentorsInGroup(groupId).FirstOrDefault();
            if (mentorId == 0)
            {
                try
                {
                    var admins = await botClient.GetChatAdministratorsAsync(groupId, cancellationToken);
                    mentorId = admins.FirstOrDefault(a => !a.User.IsBot)?.User.Id ?? 0;
                }
                catch
                {
                    mentorId = 0;
                }
            }

            var existingStudent = _students.FirstOrDefault(s => s.UserId == userId && s.GroupId == groupId);
            if (existingStudent != null)
            {
                existingStudent.FirstName = message.From.FirstName ?? "Unknown";
                existingStudent.MentorId = mentorId;
                existingStudent.LastMessageAt = DateTime.UtcNow;
            }
            else
            {
                _students.Add(new Student
                {
                    UserId = userId,
                    FirstName = message.From.FirstName ?? "Unknown",
                    GroupId = groupId,
                    MentorId = mentorId,
                    LastMessageAt = DateTime.UtcNow
                });

                if (mentorId != 0)
                {
                    await new CapacityHandler(_mentorService, this, _userService)
                        .HandleNewStudentAsync(botClient, mentorId, cancellationToken);
                }
            }

            await FetchGroupMembersAsync(botClient, groupId, cancellationToken);
            await SaveStudentsAsync();
        }

        private async Task FetchGroupMembersAsync(ITelegramBotClient botClient, long groupId, CancellationToken cancellationToken)
        {
            try
            {
                var admins = await botClient.GetChatAdministratorsAsync(groupId, cancellationToken);
                var adminIds = admins.Select(a => a.User.Id).ToHashSet();

                foreach (var student in _students.Where(s => s.GroupId == groupId))
                {
                    if (!adminIds.Contains(student.UserId) && _userService.GetUserRole(student.UserId, groupId) == UserRole.Student)
                    {
                        student.LastMessageAt = DateTime.UtcNow;
                    }
                }
            }
            catch
            {
                // Handle if bot is not admin
            }
        }

        public async Task SaveStudentsAsync()
        {
            var json = JsonSerializer.Serialize(_students, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_studentsStoragePath)!);
            await File.WriteAllTextAsync(_studentsStoragePath, json);
        }

        public IEnumerable<Student> GetAllStudents() => _students.AsReadOnly();
        public Student GetRandomStudent(long groupId) => _students
            .Where(s => s.GroupId == groupId && _userService.GetUserRole(s.UserId, groupId) == UserRole.Student)
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefault();
        public bool IsStudent(long userId, long groupId) => _students.Any(s => s.UserId == userId && s.GroupId == groupId);
        public Student GetStudentInfo(long userId) => _students
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastMessageAt)
            .FirstOrDefault();
        public void StartPendingAction(long userId, string action, int messageId) => _pendingActions.Add((userId, action, messageId));
        public bool IsPendingAction(long userId, string action, int messageId) => _pendingActions.Any(p => p.UserId == userId && p.Action == action && p.MessageId == messageId);
        public int GetPendingActionMessageId(long userId, string action) => _pendingActions.FirstOrDefault(p => p.UserId == userId && p.Action == action).MessageId;
        public void CompletePendingAction(long userId, string action, int messageId)
        {
            var pending = _pendingActions.FirstOrDefault(p => p.UserId == userId && p.Action == action && p.MessageId == messageId);
            if (pending != default)
            {
                _pendingActions.Remove(pending);
            }
        }
    }
}
