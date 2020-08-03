using Microsoft.EntityFrameworkCore;

namespace AuthBot.Models {
    public class AuthContext : DbContext {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
                => options.UseSqlite("Data Source=Auth.db");

        public DbSet<User> Users { get; set; }
    }
}