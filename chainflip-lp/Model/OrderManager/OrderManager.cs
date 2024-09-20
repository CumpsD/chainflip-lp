namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;
    using Telegram.Bot.Requests;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;

    public partial class OrderManager
    {
        private readonly ILogger _logger;
        private readonly BotConfiguration _configuration;

        private readonly ReactionTypeEmoji _tadaEmoji = new() { Emoji = "🎉" };

        public OrderManager(
            ILogger logger,
            BotConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        public async Task PlaceOrders(
            IReadOnlyDictionary<string, Dictionary<string, double>> balances,
            AccountBalances balance,
            HttpClient client,
            TelegramBotClient telegramClient,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Placing Orders");
            _logger.LogInformation("--------------");
            
            await PlaceSellOrders(
                balance, 
                client, 
                telegramClient,
                cancellationToken);
            
            await PlaceBuyOrders(
                balances, 
                balance, 
                client, 
                telegramClient,
                cancellationToken);
        }

        public async Task UpdateOrders(
            List<PoolOrders> ourPoolOrders, 
            List<PoolOrders> allPoolOrders, 
            HttpClient client,
            TelegramBotClient telegramClient, 
            CancellationToken cancellationToken)
        {
            // Compare our orders to pool orders and make them competitive
            _logger.LogInformation("Updating Orders");
            _logger.LogInformation("--------------");

            foreach (var pool in ourPoolOrders)
            {
                await UpdateSellOrders(
                    pool, 
                    allPoolOrders,
                    client, 
                    telegramClient,
                    cancellationToken);
                
                await UpdateBuyOrders(
                    pool, 
                    allPoolOrders,
                    client, 
                    telegramClient,
                    cancellationToken);
            }
        }

        private async Task NotifyTelegram(
            ITelegramBotClient telegramClient,
            string text,
            CancellationToken ct)
        {
            try
            {
                var message = await telegramClient
                    .SendMessageAsync(
                        new SendMessageRequest
                        {
                            ChatId = new ChatId(_configuration.TelegramChannelId.Value),
                            Text = text,
                            ParseMode = ParseMode.Markdown,
                            DisableNotification = true,
                            LinkPreviewOptions = new LinkPreviewOptions
                            {
                                IsDisabled = true
                            },
                            ReplyParameters = new ReplyParameters
                            {
                                AllowSendingWithoutReply = true,
                            }
                        },
                        ct);

                _logger.LogInformation(
                    "Telegram message {MessageId} sent.",
                    message.MessageId);
                
                await telegramClient
                    .SetMessageReactionAsync(
                        new SetMessageReactionRequest
                        {
                            ChatId = new ChatId(_configuration.TelegramChannelId.Value),
                            MessageId = message.MessageId,
                            Reaction = new[] { _tadaEmoji },
                            IsBig = false
                        },
                        ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Telegram Meh.");
            }
        }
    }
}