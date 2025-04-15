using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Services;
using TikBot.Models;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class ReportNotSentHandler
    {
        private readonly PollService _pollService;
        private readonly StudentService _studentService;

        public ReportNotSentHandler(PollService pollService, StudentService studentService)
        {
            _pollService = pollService;
            _studentService = studentService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;
            var userId = message.From.Id;

            // Check 3-hour restriction
            if (!_pollService.CanUseReportNotSent(userId, chatId, out var errorMessage))
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

            // Get random student
            var student = _studentService.GetRandomStudent(chatId);
            var studentText = student != null
                ? $"[{student.FirstName}](tg://user?id={student.UserId})"
                : "سلام";

            // Send message
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"{studentText}\nدانش‌آموز عزیز لطفاً در ارسال گزارش کارت منظم و دقیق‌تر باش",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            // Delete command message
            await Task.Delay(5000, cancellationToken);
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

            // Record usage
            _pollService.RecordPollResponse(userId, chatId, PollType.ReportNotSent);
        }
    }
}
