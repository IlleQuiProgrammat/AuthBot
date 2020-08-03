using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.WebSocket;
using AuthBot.Models;
using DSharpPlus.Entities;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Net;

namespace AuthBot {
    class Bot {
        private readonly DSharpPlus.DiscordClient _client;
        private readonly BotConfig _config;
        private readonly DiscordConfiguration _discordConfig;
        private readonly SmtpClient _mailClient;
        private AuthContext _context;

        public Bot(BotConfig config) {
            _config = config;
            _discordConfig = new DiscordConfiguration{
                Token=_config.BotToken,
                TokenType=TokenType.Bot,

                AutoReconnect=true,
                LogLevel=LogLevel.Debug,
                UseInternalLogHandler=true
            };
            _client = new DiscordClient(_discordConfig);

            _client.Ready += this.Client_Ready;
            _client.ClientErrored += this.Client_Error;
            _client.MessageCreated += this.Client_OnMessage;

            _mailClient = new SmtpClient(_config.SmtpSettings.SmtpServer) {
                Port=_config.SmtpSettings.SmtpPort,
                Credentials = new NetworkCredential(_config.SmtpSettings.Username, _config.SmtpSettings.Password),
                EnableSsl=_config.SmtpSettings.Ssl               
            };

            _context = new AuthContext();
        }
        public async Task Run() {
            await _client.ConnectAsync();
        }

        private Task Client_Ready(ReadyEventArgs args) {
            args.Client.DebugLogger.LogMessage(LogLevel.Info, "AuthBot", "Client is ready", DateTime.Now);
            return Task.CompletedTask; // to comply with the callback signature
        }

        private Task Client_Error(ClientErrorEventArgs args) {
            args.Client.DebugLogger.LogMessage(LogLevel.Error, "AuthBot", $"ERROR: {args.Exception.Message}", DateTime.Now);
            return Task.CompletedTask; // to comply with the callback signature
        }

        private async Task Client_OnMessage(MessageCreateEventArgs messageCreation) {
            switch (messageCreation.Message.Content.Split(' ', 2)[1].Substring(_config.Prefix.Length)) {
                case "auth":
                    await AuthenticateUser(messageCreation);
                    break;
                case "help":
                    await SendHelp(messageCreation.Channel);
                    break;
                case "generate_tokens":
                    await GenerateAuthTokens(messageCreation);
                    break;
                case "request_token":
                    await ResetUserTokens(messageCreation);
                    break;
                default:
                    await SendError(messageCreation, "Command not found");
                    break;
            }
        }

        private async Task AuthenticateUser(MessageCreateEventArgs messageCreation) {
            string[] parameters = messageCreation.Message.Content.Split(" ", 2);
            if (parameters.Length <= 1) {
                await SendError(messageCreation, $"Usage: `{_config.Prefix}auth <OTP>`");
            } else {
                var user = _context.Users.Single(u => u.KeyCode == parameters[1]);
                if (user == null) {
                    await SendError(messageCreation, "No user found, please try again.");
                } else if (user.DiscordId != null || user.DiscordId.Length > 0) {
                    await SendError(messageCreation, "KeyCode already assigned to a user.");                        
                } else {
                    user.DiscordId = messageCreation.Author.Id.ToString();
                    user.KeyCode = GenerateRandomSequence(32);
                    await _context.SaveChangesAsync();

                    var guildUser = await messageCreation.Guild.GetMemberAsync(messageCreation.Author.Id);

                    await guildUser.GrantRoleAsync(
                        messageCreation.Guild.GetRole(_config.AccessRoleId),
                        "Authenticated with AuthBot");
                    
                    if (_config.ChangeUsernames) {
                        await guildUser.ModifyAsync(nickname: user.Name,
                            reason: "Authenticated with AuthBot and this instance has `ChangeUsernames` set to `true`");
                    }
                }
            }
        }

        private async Task SendError(MessageCreateEventArgs messageCreation, string errorMessage) {
            await messageCreation.Message.RespondAsync(errorMessage);
        }

        private async Task SendHelp(DiscordChannel channel) {
            await channel.SendMessageAsync("Please see the documentation for help.");
        }

        private async Task GenerateAuthTokens(MessageCreateEventArgs messageCreation) => await ResetUserTokens(messageCreation);

        private async Task ResetUserTokens(MessageCreateEventArgs messageCreation) {
            if (
                (await messageCreation.Guild.GetMemberAsync(messageCreation.Author.Id))
                    .Roles.Contains(
                        messageCreation.Guild.GetRole(_config.AdminRoleId)
            ))
            {
                string[] arguments = messageCreation.Message.Content.Split(' ', 2);
                if (arguments.Length <= 1) {
                    await messageCreation.Message.RespondAsync(
                        $"`{_config.Prefix}generate_tokens all`: Regenerate tokens" +
                        " for all users who have not yet been authenticated");
                } else if (arguments[1] == "all") {
                    foreach (var user in _context.Users) {
                        await SendCodeByEmail(user);
                    }
                } else {
                    await messageCreation.Message.RespondAsync(
                        $"`{_config.Prefix}generate_tokens all`: Regenerate tokens" +
                        " for all users who have not yet been authenticated");
                }
            }
        }

        private async Task SendCodeByEmail(User user) {
            if (user.DiscordId != null || user.DiscordId.Length > 0) return;

            user.KeyCode = GenerateRandomSequence(32);
            await _context.SaveChangesAsync();
            
            var message = new MailMessage();
            message.From = new MailAddress(_config.SmtpSettings.Username);  
            message.To.Add(new MailAddress(user.Email));
            message.Subject = "New Auth Code for Discord";
            message.Body = $"Here is your new code: <pre>{user.KeyCode}</pre>";
            await _mailClient.SendMailAsync(message);
        }

        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        public static string GenerateRandomSequence(int length) {
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}