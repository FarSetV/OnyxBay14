﻿using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.DiscordWebhooks;
using Content.Server.DiscordWebhooks.Webhooks;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly DiscordWebhooksManager _webhooks = default!;

        private ISawmill _sawmill = default!;
        private string _webhookUrl = string.Empty;
        private WebhookData? _webhookData;
        private string _serverName = string.Empty;
        private readonly Dictionary<NetUserId, (string? id, string username, string description, string? characterName, GameRunLevel lastRunLevel)> _relayMessages = new();
        private Dictionary<NetUserId, string> _oldMessageIds = new();
        private readonly Dictionary<NetUserId, Queue<string>> _messageQueues = new();
        private readonly HashSet<NetUserId> _processingChannels = new();

        // Text to be used to cut off messages that are too long. Should be shorter than MessageLengthCap
        private const string TooLongText = "... **(too long)**";

        private int _maxAdditionalChars;

        public override void Initialize()
        {
            base.Initialize();

            _config.OnValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged, true);
            _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");
            _maxAdditionalChars = GenerateAHelpMessage("", "", true).Length;

            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        }

        private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
        {
            // Don't make a new embed if we
            // 1. were in the lobby just now, and
            // 2. are not entering the lobby or directly into a new round.
            if (args.Old is GameRunLevel.PreRoundLobby ||
                args.New is not (GameRunLevel.PreRoundLobby or GameRunLevel.InRound))
            {
                return;
            }

            // Store the Discord message IDs of the previous round
            _oldMessageIds = new Dictionary<NetUserId, string>();
            foreach (var message in _relayMessages)
            {
                var id = message.Value.id;
                if (id == null)
                    return;

                _oldMessageIds[message.Key] = id;
            }

            _relayMessages.Clear();
        }

        private void OnServerNameChanged(string obj)
        {
            _serverName = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _config.UnsubValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged);
            _config.UnsubValueChanged(CVars.GameHostName, OnServerNameChanged);
        }

        private void OnWebhookChanged(string url)
        {
            _webhookUrl = url;

            if (url == string.Empty)
                return;

            // Fire and forget
            _ = SetWebhookData(url);
        }

        private async Task SetWebhookData(string url)
        {
            _webhookData = await _webhooks.GetWebhookData(url);
        }

        private async void ProcessQueue(NetUserId userId, Queue<string> messages)
        {
            // Whether an embed already exists for this player
            var exists = _relayMessages.TryGetValue(userId, out var existingEmbed);

            // Whether the message will become too long after adding these new messages
            var tooLong = exists && messages.Sum(msg => Math.Min(msg.Length, DiscordWebhooksManager.MessageLengthCap) + "\n".Length)
                    + existingEmbed.description.Length > DiscordWebhooksManager.DescriptionMax;

            // If there is no existing embed, or it is getting too long, we create a new embed
            if (!exists || tooLong)
            {
                var lookup = await _playerLocator.LookupIdAsync(userId);

                if (lookup == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Unable to find player for NetUserId {userId} when sending discord webhook.");
                    _relayMessages.Remove(userId);
                    return;
                }

                var characterName = _playerManager.GetPlayerData(userId).ContentData()?.Mind?.CharacterName;

                var linkToPrevious = string.Empty;

                // If we have all the data required, we can link to the embed of the previous round or embed that was too long
                if (_webhookData is { GuildId: { } guildId, ChannelId: { } channelId })
                {
                    if (tooLong && existingEmbed.id != null)
                    {
                        linkToPrevious = $"**[Go to previous embed of this round](https://discord.com/channels/{guildId}/{channelId}/{existingEmbed.id})**\n";
                    }
                    else if (_oldMessageIds.TryGetValue(userId, out var id) && !string.IsNullOrEmpty(id))
                    {
                        linkToPrevious = $"**[Go to last round's conversation with this player](https://discord.com/channels/{guildId}/{channelId}/{id})**\n";
                    }
                }

                existingEmbed = (null, lookup.Username, linkToPrevious, characterName, _gameTicker.RunLevel);
            }

            // Previous message was in another RunLevel, so show that in the embed
            if (existingEmbed.lastRunLevel != _gameTicker.RunLevel)
            {
                existingEmbed.description += _gameTicker.RunLevel switch
                {
                    GameRunLevel.PreRoundLobby => "\n\n:arrow_forward: _**Pre-round lobby started**_\n",
                    GameRunLevel.InRound => "\n\n:arrow_forward: _**Round started**_\n",
                    GameRunLevel.PostRound => "\n\n:stop_button: _**Post-round started**_\n",
                    _ => throw new ArgumentOutOfRangeException(nameof(_gameTicker.RunLevel), $"{_gameTicker.RunLevel} was not matched."),
                };

                existingEmbed.lastRunLevel = _gameTicker.RunLevel;
            }

            // Add available messages to the embed description
            while (messages.TryDequeue(out var message))
            {
                // In case someone thinks they're funny
                if (message.Length > DiscordWebhooksManager.MessageLengthCap)
                    message = message[..(DiscordWebhooksManager.MessageLengthCap - TooLongText.Length)] + TooLongText;

                existingEmbed.description += $"\n{message}";
            }

            var payload = GeneratePayload(existingEmbed.description, existingEmbed.username, existingEmbed.characterName);

            // If there is no existing embed, create a new one
            // Otherwise patch (edit) it
            if (existingEmbed.id == null)
            {
                var response = await _webhooks.SendAsync(_webhookUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    _relayMessages.Remove(userId);
                    return;
                }

                var id = await DiscordWebhooksManager.TryGetId(response);
                if (id == null)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
                    _relayMessages.Remove(userId);
                    return;
                }

                existingEmbed.id = id;
            }
            else
            {
                var request = await _webhooks.PatchAsync(_webhookUrl, existingEmbed.id, payload);

                if (!request.IsSuccessStatusCode)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when patching message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(userId);
                    return;
                }
            }

            _relayMessages[userId] = existingEmbed;

            _processingChannels.Remove(userId);
        }

        private WebhookPayload GeneratePayload(string messages, string username, string? characterName = null)
        {
            // Add character name
            if (characterName != null)
                username += $" ({characterName})";

            // If no admins are online, set embed color to red. Otherwise green
            var color = GetTargetAdmins().Count > 0 ? 0x41F097 : 0xFF0000;

            // Limit server name to 1500 characters, in case someone tries to be a little funny
            var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];

            var round = _gameTicker.RunLevel switch
            {
                GameRunLevel.PreRoundLobby => _gameTicker.RoundId == 0
                    ? "pre-round lobby after server restart" // first round after server restart has ID == 0
                    : $"pre-round lobby for round {_gameTicker.RoundId + 1}",
                GameRunLevel.InRound => $"round {_gameTicker.RoundId}",
                GameRunLevel.PostRound => $"post-round {_gameTicker.RoundId}",
                _ => throw new ArgumentOutOfRangeException(nameof(_gameTicker.RunLevel), $"{_gameTicker.RunLevel} was not matched."),
            };

            return new WebhookPayload
            {
                Username = username,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = messages,
                        Color = color,
                        Footer = new EmbedFooter
                        {
                            Text = $"{serverName} ({round})",
                        },
                    },
                },
            };
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var userId in _messageQueues.Keys.ToArray())
            {
                if (_processingChannels.Contains(userId))
                    continue;

                var queue = _messageQueues[userId];
                _messageQueues.Remove(userId);
                if (queue.Count == 0)
                    continue;

                _processingChannels.Add(userId);

                ProcessQueue(userId, queue);
            }
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = (IPlayerSession) eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            var personalChannel = senderSession.UserId == message.UserId;
            var senderAdmin = _adminManager.GetAdminData(senderSession);
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var authorized = personalChannel || senderAHelpAdmin;
            if (!authorized)
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            var bwoinkText = senderAdmin switch
            {
                var x when x is not null && x.Flags == AdminFlags.Adminhelp =>
                    $"[color=purple]{senderSession.Name}[/color]: {escapedText}",
                var x when x is not null && x.HasFlag(AdminFlags.Adminhelp) =>
                    $"[color=red]{senderSession.Name}[/color]: {escapedText}",
                _ => $"{senderSession.Name}: {escapedText}",
            };

            var msg = new BwoinkTextMessage(message.UserId, senderSession.UserId, bwoinkText);

            LogBwoink(msg);

            var admins = GetTargetAdmins();

            // Notify all admins
            foreach (var channel in admins)
            {
                RaiseNetworkEvent(msg, channel);
            }

            // Notify player
            if (_playerManager.TryGetSessionById(message.UserId, out var session))
            {
                if (!admins.Contains(session.ConnectedClient))
                    RaiseNetworkEvent(msg, session.ConnectedClient);
            }

            var sendsWebhook = _webhookUrl != string.Empty;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.UserId))
                    _messageQueues[msg.UserId] = new Queue<string>();

                var str = message.Text;
                var unameLength = senderSession.Name.Length;

                if (unameLength + str.Length + _maxAdditionalChars > DiscordWebhooksManager.DescriptionMax)
                {
                    str = str[..(DiscordWebhooksManager.DescriptionMax - _maxAdditionalChars - unameLength)];
                }
                _messageQueues[msg.UserId].Enqueue(GenerateAHelpMessage(senderSession.Name, str, !personalChannel, admins.Count == 0));
            }

            if (admins.Count != 0)
                return;

            // No admin online, let the player know
            var systemText = sendsWebhook ?
                Loc.GetString("bwoink-system-starmute-message-no-other-users-webhook") :
                Loc.GetString("bwoink-system-starmute-message-no-other-users");
            var starMuteMsg = new BwoinkTextMessage(message.UserId, SystemUserId, systemText);
            RaiseNetworkEvent(starMuteMsg, senderSession.ConnectedClient);
        }

        // Returns all online admins with AHelp access
        private IList<INetChannel> GetTargetAdmins()
        {
            return _adminManager.ActiveAdmins
               .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
               .Select(p => p.ConnectedClient)
               .ToList();
        }

        private static string GenerateAHelpMessage(string username, string message, bool admin, bool noReceivers = false)
        {
            var stringbuilder = new StringBuilder();

            if (admin)
                stringbuilder.Append(":outbox_tray:");
            else if (noReceivers)
                stringbuilder.Append(":sos:");
            else
                stringbuilder.Append(":inbox_tray:");

            stringbuilder.Append($" **{username}:** ");
            stringbuilder.Append(message);
            return stringbuilder.ToString();
        }
    }
}

