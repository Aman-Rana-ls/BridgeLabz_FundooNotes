using Microsoft.EntityFrameworkCore;
using RepoLayer.Entity;

namespace RepoLayer.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Note> Notes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<NoteLabel> NoteLabels { get; set; }

        public DbSet<NoteCollaborator> NoteCollaborators { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the one-to-many relationship between User and Note
            modelBuilder.Entity<Note>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.CreatedBy)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the many-to-many relationship between Note and Label
            modelBuilder.Entity<NoteLabel>()
                .HasKey(nl => new { nl.NoteId, nl.LabelId });

            modelBuilder.Entity<NoteLabel>()
                .HasOne(nl => nl.Note)
                .WithMany(n => n.NoteLabels)
                .HasForeignKey(nl => nl.NoteId);

            modelBuilder.Entity<NoteLabel>()
                .HasOne(nl => nl.Label)
                .WithMany(l => l.NoteLabels)
                .HasForeignKey(nl => nl.LabelId);

            // Configure the many-to-many relationship between Note and User (Collaborators)
            modelBuilder.Entity<NoteCollaborator>()
                .HasKey(nc => new { nc.NoteId, nc.UserId });

            modelBuilder.Entity<NoteCollaborator>()
                .HasOne(nc => nc.Note)
                .WithMany(n => n.Collaborators)
                .HasForeignKey(nc => nc.NoteId);

            modelBuilder.Entity<NoteCollaborator>()
                .HasOne(nc => nc.User)
                .WithMany(u => u.CollaboratedNotes)
                .HasForeignKey(nc => nc.UserId);
        }
    }
}