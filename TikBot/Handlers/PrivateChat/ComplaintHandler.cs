﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.PrivateChat
{
    public class ComplaintHandler
    {
        private readonly StudentService _studentService;
        private readonly UserService _userService;
        private const long COMPLAINT_GROUP_CHAT_ID = -4652850967;

        public ComplaintHandler(StudentService studentService, UserService userService)
        {
            _studentService = studentService;
            _userService = userService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;

            // Ask for complaint
            var askMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "اگه مشکل در دریافت خدمات داشتی و می‌خوای فورا حل بشه تو یک پیام شکایتت رو ارسال کن",
                cancellationToken: cancellationToken
            );

            // Mark as pending
            _studentService.StartPendingAction(userId, "Complaint", askMessage.MessageId);
        }

        public async Task HandleResponseAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            // Check if this is a pending complaint
            var pendingMessageId = _studentService.GetPendingActionMessageId(userId, "Complaint");
            if (!_studentService.IsPendingAction(userId, "Complaint", pendingMessageId))
            {
                return;
            }

            // Get student info
            var student = _studentService.GetStudentInfo(userId);
            if (student == null || student.GroupId == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "شما در گروهی ثبت‌نام نکردید. لطفاً در گروه پیشرفت پیام دهید.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Get group info
            var (groupName, groupLink) = await GetGroupInfoAsync(botClient, student.GroupId, cancellationToken);
            var mentorId = student.MentorId;
            var mentorName = _userService.GetMentor(mentorId)?.FullName ?? "منتور ناشناس";
            var mentorLink = $"tg://user?id={mentorId}";

            // Send confirmation
            var confirmationMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "از نارضایتی شما خیلی متأسفیم\nپیام شما برای دکتر احمدی ارسال و خیلی سریع پیگیری می‌شه",
                cancellationToken: cancellationToken
            );

            // Send to complaint group
            var complaintMessage = $"اسم گروه: [{groupName}]({groupLink})\n" +
                                  $"اسم منتور: [{mentorName}]({mentorLink})\n" +
                                  "موضوع: شکایت از خدمات\n" +
                                  $"دلیل: {message.Text ?? "بدون توضیح"}";

            await botClient.SendTextMessageAsync(
                chatId: COMPLAINT_GROUP_CHAT_ID,
                text: complaintMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            // Delete messages
            await botClient.DeleteMessageAsync(chatId, pendingMessageId, cancellationToken); // "شکایتت رو بنویس"
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken); // User message
            await botClient.DeleteMessageAsync(chatId, confirmationMessage.MessageId, cancellationToken); // Confirmation

            // Clear pending
            _studentService.CompletePendingAction(userId, "Complaint", pendingMessageId);
        }

        private async Task<(string GroupName, string GroupLink)> GetGroupInfoAsync(ITelegramBotClient botClient, long groupId, CancellationToken cancellationToken)
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
    }
}
