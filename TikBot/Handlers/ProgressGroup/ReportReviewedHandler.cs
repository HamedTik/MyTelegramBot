using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TikBot.Services;
using TikBot.Models;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class ReportReviewedHandler
    {
        private readonly TicketService _ticketService;
        private readonly PollService _pollService;
        private readonly UserService _userService;

        public ReportReviewedHandler(TicketService ticketService, PollService pollService, UserService userService)
        {
            _ticketService = ticketService;
            _pollService = pollService;
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

            // Check 1-hour restriction for mentors
            if (!_pollService.CanUseReportReviewed(userId, chatId, out var errorMessage))
            {
                var warningMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: errorMessage,
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, warningMessage.MessageId, cancellationToken);
                return;
            }

            // Send thank you message
            var thankYouMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "ممنون از بررسی گزارشت\nخسته نمونی",
                cancellationToken: cancellationToken
            );

            // Delete both messages after 5 seconds
            await Task.Delay(5000, cancellationToken);
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
            await botClient.DeleteMessageAsync(chatId, thankYouMessage.MessageId, cancellationToken);

            // Send poll to student's private chat
            var student = _ticketService.GetRandomStudent(chatId); // Assuming TicketService has access to StudentService
            if (student == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "دانش‌آموزی در گروه یافت نشد.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var pollKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("1", "poll_1"), InlineKeyboardButton.WithCallbackData("2", "poll_2") },
                new[] { InlineKeyboardButton.WithCallbackData("3", "poll_3"), InlineKeyboardButton.WithCallbackData("4", "poll_4") },
                new[] { InlineKeyboardButton.WithCallbackData("5", "poll_5"), InlineKeyboardButton.WithCallbackData("6", "poll_6") },
                new[] { InlineKeyboardButton.WithCallbackData("7", "poll_7"), InlineKeyboardButton.WithCallbackData("8", "poll_8") },
                new[] { InlineKeyboardButton.WithCallbackData("9", "poll_9"), InlineKeyboardButton.WithCallbackData("10", "poll_10") }
            });

            await botClient.SendTextMessageAsync(
                chatId: student.UserId, // Send to student's private chat
                text: "دانش‌آموز عزیز\nبه بررسی گزارش کارت توسط منتور چه نمره‌ای میدی؟",
                replyMarkup: pollKeyboard,
                cancellationToken: cancellationToken
            );

            // Record report reviewed for ticket eligibility
            if (student != null)
            {
                _ticketService.RecordReportReviewed(student.UserId, chatId);
            }

            // Record poll usage for mentor
            _pollService.RecordPollResponse(userId, chatId, PollType.ReportReviewed);
        }
    }
}
