using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TikBot.Models;
using TikBot.Services;

namespace TikBot.Handlers.UpgradeGroup
{
    public class CapacityHandler
    {
        private readonly MentorService _mentorService;
        private readonly StudentService _studentService;
        private readonly UserService _userService;

        public CapacityHandler(MentorService mentorService, StudentService studentService, UserService userService)
        {
            _mentorService = mentorService;
            _studentService = studentService;
            _userService = userService;
        }

        // هندل کردن دکمه "ظرفیت" و ارسال نظرسنجی اول
        public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            try
            {
                var userId = callbackQuery.From.Id;
                var chatId = callbackQuery.Message.Chat.Id;

                // چک کردن نقش کاربر
                if (_userService.GetUserRole(userId, chatId) != UserRole.Mentor)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "این دکمه فقط برای منتورها فعال است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var currentStudents = _mentorService.GetCurrentStudentCount(userId);

                // ساخت کیبورد برای نظرسنجی منتور
                var mentorKeyboard = new InlineKeyboardMarkup(
                    new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("1", $"capacity_mentor_{userId}_1"), InlineKeyboardButton.WithCallbackData("2", $"capacity_mentor_{userId}_2") },
                        new[] { InlineKeyboardButton.WithCallbackData("3", $"capacity_mentor_{userId}_3"), InlineKeyboardButton.WithCallbackData("4", $"capacity_mentor_{userId}_4") },
                        new[] { InlineKeyboardButton.WithCallbackData("5", $"capacity_mentor_{userId}_5"), InlineKeyboardButton.WithCallbackData("6", $"capacity_mentor_{userId}_6") },
                        new[] { InlineKeyboardButton.WithCallbackData("7", $"capacity_mentor_{userId}_7"), InlineKeyboardButton.WithCallbackData("8", $"capacity_mentor_{userId}_8") },
                        new[] { InlineKeyboardButton.WithCallbackData("9", $"capacity_mentor_{userId}_9"), InlineKeyboardButton.WithCallbackData("10", $"capacity_mentor_{userId}_10") }
                    }
                );

                // ارسال نظرسنجی اول
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"منتور عزیز\nشما الان {currentStudents} دانش‌آموز دارید\nچند دانش‌آموز دیگر می‌توانید مدیریت کنید؟",
                    replyMarkup: mentorKeyboard,
                    cancellationToken: cancellationToken
                );

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleAsync: {ex.Message}");
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    text: "خطایی رخ داد، لطفاً دوباره امتحان کنید",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
        }

        // هندل کردن پاسخ منتور و ارسال نظرسنجی دوم
        public async Task HandleMentorCapacityResponseAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            try
            {
                var userId = callbackQuery.From.Id;
                var chatId = callbackQuery.Message.Chat.Id;
                var messageId = callbackQuery.Message.MessageId;
                var data = callbackQuery.Data;

                // چک کردن فرمت داده
                if (!data.StartsWith("capacity_mentor_"))
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "داده نامعتبر است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // چک کردن نقش کاربر
                if (_userService.GetUserRole(userId, chatId) != UserRole.Mentor)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "فقط منتورها می‌توانند به این نظرسنجی پاسخ دهند",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // گرفتن ظرفیت انتخاب‌شده
                if (!int.TryParse(data.Split('_').Last(), out var capacity))
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "مقدار ظرفیت نامعتبر است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // ثبت ظرفیت منتور
                _mentorService.RecordMentorCapacity(userId, capacity);
                await _mentorService.SaveCapacityRecordsAsync();

                // ارسال الرت تأیید
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"شما ظرفیت {capacity} را انتخاب کردید",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // پاک کردن نظرسنجی اول
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

                // گرفتن اطلاعات منتور
                var mentor = _userService.GetMentor(userId);
                var mentorName = mentor?.FullName ?? "منتور ناشناس";
                var mentorLink = $"tg://user?id={userId}";

                // ساخت کیبورد برای نظرسنجی ناظر
                var supervisorKeyboard = new InlineKeyboardMarkup(
                    new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("1", $"capacity_supervisor_{userId}_1"), InlineKeyboardButton.WithCallbackData("2", $"capacity_supervisor_{userId}_2") },
                        new[] { InlineKeyboardButton.WithCallbackData("3", $"capacity_supervisor_{userId}_3"), InlineKeyboardButton.WithCallbackData("4", $"capacity_supervisor_{userId}_4") },
                        new[] { InlineKeyboardButton.WithCallbackData("5", $"capacity_supervisor_{userId}_5"), InlineKeyboardButton.WithCallbackData("6", $"capacity_supervisor_{userId}_6") },
                        new[] { InlineKeyboardButton.WithCallbackData("7", $"capacity_supervisor_{userId}_7"), InlineKeyboardButton.WithCallbackData("8", $"capacity_supervisor_{userId}_8") },
                        new[] { InlineKeyboardButton.WithCallbackData("9", $"capacity_supervisor_{userId}_9"), InlineKeyboardButton.WithCallbackData("10", $"capacity_supervisor_{userId}_10") }
                    }
                );

                // ارسال نظرسنجی دوم برای ناظر
                var supervisorMessage = $"@PoshtiBani_Tik\nناظر عزیز، لطفاً ظرفیت پیشنهادی برای منتور [{mentorName}]({mentorLink}) را تأیید یا تغییر دهید\nظرفیت پیشنهادی منتور: {capacity}";
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: supervisorMessage,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: supervisorKeyboard,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleMentorCapacityResponseAsync: {ex.Message}");
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    text: "خطایی در ارسال نظرسنجی ناظر رخ داد، لطفاً دوباره امتحان کنید",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
        }

        // هندل کردن پاسخ ناظر و ارسال لیست
        public async Task HandleSupervisorCapacityResponseAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            try
            {
                var userId = callbackQuery.From.Id;
                var chatId = callbackQuery.Message.Chat.Id;
                var messageId = callbackQuery.Message.MessageId;
                var dataParts = callbackQuery.Data.Split('_');

                // چک کردن فرمت داده
                if (dataParts.Length < 4 || !dataParts[0].Equals("capacity") || !dataParts[1].Equals("supervisor"))
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "داده نامعتبر است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // گرفتن شناسه منتور و ظرفیت
                if (!long.TryParse(dataParts[2], out var mentorId))
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "شناسه منتور نامعتبر است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                if (!int.TryParse(dataParts[3], out var capacity))
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "مقدار ظرفیت نامعتبر است",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // چک کردن نقش کاربر
                if (_userService.GetUserRole(userId, chatId) != UserRole.Supervisor)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "فقط ناظران می‌توانند به این نظرسنجی پاسخ دهند",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // ثبت ظرفیت ناظر
                _mentorService.RecordSupervisorCapacity(mentorId, capacity);
                await _mentorService.SaveCapacityRecordsAsync();

                // ارسال الرت تأیید
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"شما ظرفیت {capacity} را برای منتور تأیید کردید",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // پاک کردن نظرسنجی دوم
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

                // ارسال پیام تأیید به منتور
                var mentor = _userService.GetMentor(mentorId);
                var mentorChatId = mentor?.UpgradeGroupId ?? chatId;
                var confirmationMessage = $"دستیار دکتر احمدی ظرفیت {capacity} را برای شما تأیید کرد و به‌زودی اعمال می‌شود";
                await botClient.SendTextMessageAsync(
                    chatId: mentorChatId,
                    text: confirmationMessage,
                    cancellationToken: cancellationToken
                );

                // ساخت و ارسال لیست ظرفیت‌ها
                var capacityRecords = _mentorService.GetCapacityRecords();
                var capacityMessage = new StringBuilder();
                if (capacityRecords.Any())
                {
                    foreach (var record in capacityRecords)
                    {
                        var m = _userService.GetMentor(record.MentorId);
                        var mentorName = m?.FullName ?? "منتور ناشناس";
                        capacityMessage.AppendLine($"{mentorName}: {record.SupervisorApprovedCapacity}");
                    }
                }
                else
                {
                    capacityMessage.AppendLine("هیچ رکوردی برای ظرفیت وجود ندارد.");
                }

                var finalMessage = capacityMessage.ToString().Trim();
                if (!string.IsNullOrEmpty(finalMessage))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: -4732797962,
                        text: finalMessage,
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleSupervisorCapacityResponseAsync: {ex.Message}");
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    text: "خطایی رخ داد، لطفاً دوباره امتحان کنید",
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
        }

        // هندل کردن افزودن دانش‌آموز جدید
        public async Task HandleNewStudentAsync(ITelegramBotClient botClient, long mentorId, CancellationToken cancellationToken)
        {
            try
            {
                _mentorService.AddStudentToMentor(mentorId);
                await _mentorService.SaveCapacityRecordsAsync();

                var remainingCapacity = _mentorService.GetRemainingCapacity(mentorId);
                var message = remainingCapacity > 0
                    ? $"شما یک دانش‌آموز جدید گرفتید\nظرفیت باقی‌مانده: {remainingCapacity}"
                    : "شما یک دانش‌آموز جدید گرفتید\nظرفیت تکمیل شد";

                var mentor = _userService.GetMentor(mentorId);
                var mentorChatId = mentor?.UpgradeGroupId ?? -4732797962;
                await botClient.SendTextMessageAsync(
                    chatId: mentorChatId,
                    text: message,
                    cancellationToken: cancellationToken
                );

                var capacityRecords = _mentorService.GetCapacityRecords();
                var capacityMessage = new StringBuilder();
                if (capacityRecords.Any())
                {
                    foreach (var record in capacityRecords)
                    {
                        var m = _userService.GetMentor(record.MentorId);
                        var mentorName = m?.FullName ?? "منتور ناشناس";
                        capacityMessage.AppendLine($"{mentorName}: {record.SupervisorApprovedCapacity}");
                    }
                }
                else
                {
                    capacityMessage.AppendLine("هیچ رکوردی برای ظرفیت وجود ندارد.");
                }

                var finalMessage = capacityMessage.ToString().Trim();
                if (!string.IsNullOrEmpty(finalMessage))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: -4732797962,
                        text: finalMessage,
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleNewStudentAsync: {ex.Message}");
                var mentor = _userService.GetMentor(mentorId);
                var mentorChatId = mentor?.UpgradeGroupId ?? -4732797962;
                await botClient.SendTextMessageAsync(
                    chatId: mentorChatId,
                    text: "خطایی در افزودن دانش‌آموز جدید رخ داد",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
