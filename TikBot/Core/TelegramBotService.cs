using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TikBot.Services;

namespace TikBot.Core
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UserService _userService;
        private readonly GroupService _groupService;
        private readonly MessageService _messageService;
        private readonly Action<string> _updateStatus;
        private CancellationTokenSource _receiveCts;

        public TelegramBotService(
            string token,
            UserService userService,
            GroupService groupService,
            MessageService messageService,
            Action<string> updateStatus)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token), "Bot token cannot be null or empty.");
            }

            try
            {
                _botClient = new TelegramBotClient(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize TelegramBotClient: {ex.Message}", ex);
            }

            _userService = userService;
            _groupService = groupService;
            _messageService = messageService;
            _updateStatus = updateStatus;
            _receiveCts = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var me = await _botClient.GetMeAsync(cancellationToken);
                _updateStatus($"Bot started: @{me.Username}");

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { } // همه آپدیت‌ها رو بگیر
                };

                await StartReceivingAsync(receiverOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _updateStatus($"Failed to start bot: {ex.Message}");
                Console.WriteLine($"Start error: {ex.Message}");
                throw;
            }
        }

        private async Task StartReceivingAsync(ReceiverOptions receiverOptions, CancellationToken cancellationToken)
        {
            try
            {
                _botClient.StartReceiving(
                    async (botClient, update, ct) =>
                    {
                        try
                        {
                            if (update.Message != null)
                            {
                                await _messageService.HandleMessageAsync(botClient, update.Message, ct);
                            }
                            else if (update.CallbackQuery != null)
                            {
                                await _messageService.HandleCallbackQueryAsync(botClient, update.CallbackQuery, ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            _updateStatus($"Error handling update: {ex.Message}");
                            Console.WriteLine($"Update error: {ex.Message} - {ex.StackTrace}");
                        }
                    },
                    async (botClient, exception, ct) =>
                    {
                        _updateStatus($"Receiver error: {exception.Message}");
                        Console.WriteLine($"Receiver error: {exception.Message} - {exception.StackTrace}");
                        await Task.Delay(1000, ct); // صبر قبل از retry
                    },
                    receiverOptions,
                    _receiveCts.Token
                );
            }
            catch (Exception ex)
            {
                _updateStatus($"Error starting receiver: {ex.Message}");
                Console.WriteLine($"Receiver start error: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _receiveCts.Cancel();
                _receiveCts.Dispose();
                _receiveCts = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping bot: {ex.Message}");
            }
        }

        public async Task RestartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Stop();
                _updateStatus("Restarting bot...");
                await StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _updateStatus($"Failed to restart bot: {ex.Message}");
                Console.WriteLine($"Restart error: {ex.Message}");
            }
        }
    }
}
