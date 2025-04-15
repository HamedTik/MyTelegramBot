﻿using System;
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
    public class ProgramSentHandler
    {
        private readonly StudentService _studentService;
        private readonly UserService _userService;

        public ProgramSentHandler(StudentService studentService, UserService userService)
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
                text: "ممنون از برنامت\nخسته نباشی",
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
                new[] { InlineKeyboardButton.WithCallbackData("1", "program_score_1"), InlineKeyboardButton.WithCallbackData("2", "program_score_2") },
                new[] { InlineKeyboardButton.WithCallbackData("3", "program_score_3"), InlineKeyboardButton.WithCallbackData("4", "program_score_4") },
                new[] { InlineKeyboardButton.WithCallbackData("5", "program_score_5"), InlineKeyboardButton.WithCallbackData("6", "program_score_6") },
                new[] { InlineKeyboardButton.WithCallbackData("7", "program_score_7"), InlineKeyboardButton.WithCallbackData("8", "program_score_8") },
                new[] { InlineKeyboardButton.WithCallbackData("9", "program_score_9"), InlineKeyboardButton.WithCallbackData("10", "program_score_10") }
            });

            await botClient.SendTextMessageAsync(
                chatId: student.UserId, // Send to student's private chat
                text: "به برنامه ارسالی منتورت چه نمره‌ای میدی؟",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }
    }
}
