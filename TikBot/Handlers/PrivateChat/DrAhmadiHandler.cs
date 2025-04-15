using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.PrivateChat
{
    public class DrAhmadiHandler
    {
        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var voicePath = @"C:\TikBotData\drAhmadi.ogg";

            if (!File.Exists(voicePath))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "متأسفیم، پیام صوتی در دسترس نیست.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await using var stream = File.OpenRead(voicePath);
            await botClient.SendVoiceAsync(
                chatId: chatId,
                voice: new InputFileStream(stream, "drAhmadi.ogg"), // Fixed
                caption: "پیام از دکتر احمدی",
                cancellationToken: cancellationToken
            );
        }
    }
}
