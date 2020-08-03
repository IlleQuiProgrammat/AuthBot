namespace AuthBot.Models {
    public class User {
        public int UserId { get; set; }
        public string DiscordId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string KeyCode { get; set; }
    }
}