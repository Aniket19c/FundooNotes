using Microsoft.EntityFrameworkCore;
using Models.Entity;
using Repository.Entity;

namespace Repository.Context
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> option) : base(option)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<NotesEntity> Notes { get; set; }
        public DbSet<CollaboratorEntity> Collaborators { get; set; }
        public DbSet<LabelEntity> Labels { get; set; }
        public DbSet<NoteLabelEntity> NoteLabels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<NoteLabelEntity>()
                .HasKey(nl => new { nl.NoteId, nl.LabelId });  

            modelBuilder.Entity<NoteLabelEntity>()
                .HasOne(nl => nl.Note)
                .WithMany(n => n.NoteLabels)  
                .HasForeignKey(nl => nl.NoteId);

            modelBuilder.Entity<NoteLabelEntity>()
                .HasOne(nl => nl.Label)
                .WithMany(l => l.NoteLabels) 
                .HasForeignKey(nl => nl.LabelId);
        }
    }
}
