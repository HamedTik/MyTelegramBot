using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class TicketToStudentHandler
    {
        private readonly TicketService _ticketService;
        private readonly StudentService _studentService;

        public TicketToStudentHandler(TicketService ticketService, StudentService studentService)
        {
            _ticketService = ticketService;
            _studentService = studentService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            // Get random student
            var student = _studentService.GetRandomStudent(chatId);
            if (student == null)
            {
                var warningMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "دانش‌آموزی در گروه پیدا نشد.",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, warningMessage.MessageId, cancellationToken);
                return;
            }

            var studentId = student.UserId;

            // Check ticket issuance restrictions
            if (!_ticketService.CanIssueTicket(studentId, chatId, out var errorMessage))
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

            // Issue ticket
            _ticketService.IssueTicket(studentId, chatId);
            var ticketCount = _ticketService.GetTicketCount(studentId, chatId);

            // Send sticker
            await botClient.SendStickerAsync(
                chatId: chatId,
                sticker: "CAACAgIAAxkBAAKDQWe8j9vMFh2W7HkNudt0MmiBTW6DAAIeAAOQ_ZoV5nGx8XYbDS82BA",
                cancellationToken: cancellationToken
            );

            // Send message
            var studentText = $"[{student.FirstName}](tg://user?id={studentId})";
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"{studentText}\nآفرین تیکت جدید گرفتی\nتعداد تیکت‌هات: {ticketCount}",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            // Delete command message
            await Task.Delay(5000, cancellationToken);
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
        }
    }
}
