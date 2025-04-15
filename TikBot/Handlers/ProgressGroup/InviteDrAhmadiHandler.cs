using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Models;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.ProgressGroup
{
    public class InviteDrAhmadiHandler
    {
        private readonly InviteService _inviteService;
        private readonly UserService _userService;
        private const long DR_AHMADI_CHAT_ID = 5900030627;

        public InviteDrAhmadiHandler(InviteService inviteService, UserService userService)
        {
            _inviteService = inviteService;
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

            // Check 7-day restriction
            if (!_inviteService.CanInvite(userId, chatId, out var errorMessage))
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

            // Get group info
            try
            {
                var chat = await botClient.GetChatAsync(chatId, cancellationToken);
                var groupName = chat.Title ?? "گروه بدون نام";
                var inviteLink = await _inviteService.GetGroupInviteLinkAsync(botClient, chatId, cancellationToken);

                if (string.IsNullOrEmpty(inviteLink))
                {
                    var warningMessage = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "نمی‌توان لینک دعوت گروه را دریافت کرد. لطفاً مطمئن شوید که بات دسترسی‌های لازم را دارد.",
                        cancellationToken: cancellationToken
                    );

                    await Task.Delay(5000, cancellationToken);
                    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                    await botClient.DeleteMessageAsync(chatId, warningMessage.MessageId, cancellationToken);
                    return;
                }

                // Send invite to Dr. Ahmadi
                await botClient.SendTextMessageAsync(
                    chatId: DR_AHMADI_CHAT_ID,
                    text: $"از شما در گروه\n[{groupName}]({inviteLink})\nدعوت شده",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );

                // Confirm in group
                var confirmationMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "دعوت‌نامه برای دکتر احمدی ارسال شد.",
                    cancellationToken: cancellationToken
                );

                // Delete messages after 5 seconds
                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, confirmationMessage.MessageId, cancellationToken);

                // Record invite
                _inviteService.RecordInvite(userId, chatId);
            }
            catch
            {
                var warningMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "خطا در دریافت اطلاعات گروه. لطفاً دوباره امتحان کنید.",
                    cancellationToken: cancellationToken
                );

                await Task.Delay(5000, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await botClient.DeleteMessageAsync(chatId, warningMessage.MessageId, cancellationToken);
            }
        }

    }
}
