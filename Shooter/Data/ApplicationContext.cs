using Microsoft.EntityFrameworkCore;
using Shooter.Models;

namespace Shooter.Data
{
    internal class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        public DbSet<PlayerState> PlayerStates => Set<PlayerState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerState>(b =>
            {
                b.HasKey(x => x.Nickname);
                b.Property(x => x.Nickname).IsRequired();
                b.Property(x => x.PlayerX).IsRequired();
                b.Property(x => x.PlayerY).IsRequired();
                b.Property(x => x.PlayerA).IsRequired();
                b.Property(x => x.UpdatedAt).IsRequired();
            });
        }
    }
}

