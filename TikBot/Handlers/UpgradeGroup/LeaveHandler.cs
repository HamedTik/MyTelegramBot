using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using TikBot.Models;
using TikBot.Services;

namespace TikBot.Handlers.UpgradeGroup
{
    public class LeaveHandler
    {
        private readonly MentorService _mentorService;
        private readonly StudentService _studentService;
        private readonly GroupService _groupService;
        private readonly UserService _userService;

        public LeaveHandler(MentorService mentorService, StudentService studentService, GroupService groupService, UserService userService)
        {
            _mentorService = mentorService;
            _studentService = studentService;
            _groupService = groupService;
            _userService = userService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;

            if (_userService.GetUserRole(userId, chatId) != UserRole.Mentor)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "این دکمه فقط برای منتورها فعال هست",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (_mentorService.CanTakeLeave(userId, out var errorMessage))
            {
                _mentorService.RecordLeave(userId);
                await _mentorService.SaveLeaveRecordsAsync();

                // ارسال پیام به گروه ارتقا
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "درخواست مرخصیت تایید و به دانش‌آموزانت ارسال شد",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // ارسال پیام به گروه‌های پیشرفت
                var studentGroups = _studentService.GetAllStudents()
                    .Where(s => s.MentorId == userId && _groupService.GetGroupType(s.GroupId) == GroupType.Progress)
                    .Select(s => s.GroupId)
                    .Distinct()
                    .ToList();

                foreach (var groupId in studentGroups)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: groupId,
                        text: "دانش‌آموز عزیز\nمنتورت امشب استراحته و فردا گزارشت رو چک میکنه\nولی شما مثل همیشه به کارت ادامه بده",
                        cancellationToken: cancellationToken
                    );
                }
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: errorMessage,
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
