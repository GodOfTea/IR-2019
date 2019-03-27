using Microsoft.EntityFrameworkCore;

namespace DataRetrieval
{
    public class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=84.201.147.162;Port=5432;Database=CoderLiQ;Username=developer;Password=rtfP@ssw0rd");
        }

        public DbSet<Movie> movies { get; set; }
    }
}