using System.IO;
using System.Text;

namespace AuthBot {
    class BotConfig {
        public BotConfig() {}
        public static BotConfig LoadBotConfig(string filePath) {
            // TODO: Load from file
            var json = "";
            using (var fs = File.OpenRead(filePath))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    json = sr.ReadToEnd();
            return System.Text.Json.JsonSerializer.Deserialize<BotConfig>(json);
        }

        public string BotToken { get; }
        public string Prefix { get; }

        public bool ChangeUsernames { get; }
        public ulong AccessRoleId { get; }
        public ulong AdminRoleId { get; }

        public EmailConfig SmtpSettings { get; }
    }

    class EmailConfig {
        public string SmtpServer { get; }
        public int SmtpPort { get; }
        public string Username { get; }
        public string Password { get; }
        public bool Ssl { get; }
    }
}