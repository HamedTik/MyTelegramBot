using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace TikBot.Handlers.UpgradeGroup
{
    class MyScoreHandler
    {
        public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "ظرفیت شما بررسی شد.",
                cancellationToken: cancellationToken
            );
        }
    }
}
