using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Services;
using Message = Telegram.Bot.Types.Message;

namespace TikBot.Handlers.PrivateChat
{
    public class MyTicketsHandler
    {
        private readonly TicketService _ticketService;
        private readonly GroupInfoService _groupInfoService;

        public MyTicketsHandler(TicketService ticketService, GroupInfoService groupInfoService)
        {
            _ticketService = ticketService;
            _groupInfoService = groupInfoService;
        }

        public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var studentId = message.From.Id;

            // Get all groups for the student
            var groupIds = _groupInfoService.GetStudentGroups(studentId);
            var totalTickets = groupIds.Sum(groupId => _ticketService.GetTicketCount(studentId, groupId));

            // Send sticker
            await botClient.SendStickerAsync(
                chatId: chatId,
                sticker: "CAACAgIAAxkBAAKDSWe8kVAklfM2ZIxapSIhNXmoTBDmAAKMAAMWQmsKQo7-Yhc9TeI2BA",
                cancellationToken: cancellationToken
            );

            // Send message
            var text = totalTickets > 0
                ? $"تعداد تیکت‌های تو: {totalTickets}"
                : "فعلاً تیکتی نداری، گزارش بفرست!";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                cancellationToken: cancellationToken
            );
        }
    }
}
