using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class ContactedHandler
    {
        private readonly StudentService _studentService;
        private readonly UserService _userService;

        public ContactedHandler(StudentService studentService, UserService userService)
        {
            _studentService = studentService;
            _userService = userService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            // Delete button message
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

            // Send confirmation
            var confirmation = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "ممنون از تماست\nخسته نباشی",
                cancellationToken: cancellationToken
            );

            // Delete confirmation after 5 seconds
            await Task.Delay(5000, cancellationToken);
            await botClient.DeleteMessageAsync(chatId, confirmation.MessageId, cancellationToken);

            // Send poll to student's private chat
            var student = _studentService.GetRandomStudent(chatId);
            if (student == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "دانش‌آموزی در گروه یافت نشد.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("1", "contact_score_1"), InlineKeyboardButton.WithCallbackData("2", "contact_score_2") },
                new[] { InlineKeyboardButton.WithCallbackData("3", "contact_score_3"), InlineKeyboardButton.WithCallbackData("4", "contact_score_4") },
                new[] { InlineKeyboardButton.WithCallbackData("5", "contact_score_5"), InlineKeyboardButton.WithCallbackData("6", "contact_score_6") },
                new[] { InlineKeyboardButton.WithCallbackData("7", "contact_score_7"), InlineKeyboardButton.WithCallbackData("8", "contact_score_8") },
                new[] { InlineKeyboardButton.WithCallbackData("9", "contact_score_9"), InlineKeyboardButton.WithCallbackData("10", "contact_score_10") }
            });

            await botClient.SendTextMessageAsync(
                chatId: student.UserId, // Send to student's private chat
                text: "به تماس امروزت با منتور چه نمره‌ای میدی؟",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }
    }
}
