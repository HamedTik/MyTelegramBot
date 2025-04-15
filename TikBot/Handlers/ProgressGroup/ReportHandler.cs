using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using TikBot.Models;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class ReportHandler
    {
        private readonly ReportService _reportService;
        private readonly UserService _userService;
        private const long REPORT_GROUP_CHAT_ID = -4644007790;

        public ReportHandler(ReportService reportService, UserService userService)
        {
            _reportService = reportService;
            _userService = userService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;
            var userId = message.From.Id;

            // Check if user is a mentor
            if (_userService.GetUserRole(userId, chatId) != UserRole.Mentor)
            {
                var warningMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "فقط منتورها می‌توانند از این دکمه استفاده کنند.",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, warningMessage.MessageId, cancellationToken);
                return;
            }

            // Delete command message immediately
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

            // Send report reason poll
            var pollKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("رفتار نامحترمانه", "report_rude") },
                new[] { InlineKeyboardButton.WithCallbackData("بی‌توجهی به مشاور", "report_neglect") },
                new[] { InlineKeyboardButton.WithCallbackData("در دسترس نبودن دانش‌آموز", "report_unavailable") },
                new[] { InlineKeyboardButton.WithCallbackData("انصراف دانش‌آموز", "report_dropout") },
                new[] { InlineKeyboardButton.WithCallbackData("سایر", "report_other") }
            });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "دلیل ریپورت دانش‌آموز رو انتخاب کن",
                replyMarkup: pollKeyboard,
                cancellationToken: cancellationToken
            );
        }

        public async Task HandleCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;

            // Check if user is a mentor
            if (_userService.GetUserRole(userId, chatId) != UserRole.Mentor)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "فقط منتورها می‌توانند به این نظرسنجی پاسخ دهند.",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var reason = callbackQuery.Data switch
            {
                "report_rude" => "رفتار نامحترمانه",
                "report_neglect" => "بی‌توجهی به مشاور",
                "report_unavailable" => "در دسترس نبودن دانش‌آموز",
                "report_dropout" => "انصراف دانش‌آموز",
                "report_other" => null,
                _ => null
            };

            if (reason != null)
            {
                // Record and send report
                await SendReportAsync(botClient, userId, chatId, reason, cancellationToken);

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "ریپورت شما ثبت و پیگیری می‌شه",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // Delete poll
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
            }
            else // "سایر"
            {
                // Delete poll
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

                // Ask for reason
                var askMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "دلیل ریپورتت رو تو یک پیام برامون بنویس",
                    cancellationToken: cancellationToken
                );

                // Mark as pending
                _reportService.StartPendingReport(userId, chatId, askMessage.MessageId);
            }
        }

        public async Task HandleReportReasonAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;
            var userId = message.From.Id;

            // Check if this is a pending report response
            var pendingMessageId = _reportService.GetPendingMessageId(userId, chatId);
            if (!_reportService.IsPendingReport(userId, chatId, pendingMessageId))
            {
                return;
            }

            // Record and send report
            var reason = message.Text ?? "دلیل نامشخص";
            await SendReportAsync(botClient, userId, chatId, reason, cancellationToken);

            // Send thank you message
            var thankYouMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "ممنون از ریپورتت",
                cancellationToken: cancellationToken
            );

            // Delete all messages
            await botClient.DeleteMessageAsync(chatId, pendingMessageId, cancellationToken); // "دلیل بنویس"
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken); // User message
            await botClient.DeleteMessageAsync(chatId, thankYouMessage.MessageId, cancellationToken); // Thank you

            // Complete pending report
            _reportService.CompletePendingReport(userId, chatId, pendingMessageId);
        }

        private async Task SendReportAsync(ITelegramBotClient botClient, long userId, long chatId, string reason, CancellationToken cancellationToken)
        {
            var (groupName, groupLink) = await _reportService.GetGroupInfoAsync(botClient, chatId, cancellationToken);
            var mentorName = _reportService.GetMentorName(userId);
            var mentorLink = _reportService.GetMentorLink(userId);

            var reportMessage = $"اسم گروه: [{groupName}]({groupLink})\n" +
                               $"اسم منتور: [{mentorName}]({mentorLink})\n" +
                               "موضوع: ریپورت\n" +
                               $"دلیل: {reason}";

            _reportService.RecordReport(userId, chatId, reason);
            await botClient.SendTextMessageAsync(
                chatId: REPORT_GROUP_CHAT_ID,
                text: reportMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }
    }
}
