using Microsoft.EntityFrameworkCore;
using RepoLayer.EntityOne;

namespace RepoLayer.ContextOne
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Note> Notes { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>()
                .HasOne(n => n.User) // Navigation property to User
                .WithMany(u => u.Notes) // One User can have many Notes
                .HasForeignKey(n => n.CreatedBy) // Foreign key property
                .OnDelete(DeleteBehavior.Cascade); // When User is deleted, related Notes are also deleted
        }
    }
}
