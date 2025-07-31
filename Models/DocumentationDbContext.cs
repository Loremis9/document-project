using Microsoft.EntityFrameworkCore;

namespace WEBAPI_m1IL_1.Models
{
    public class DocumentationDbContext: DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<Documentation> Documentations { get; set; }
        public DbSet<DocumentationFile> DocumentationFiles { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DocumentationDbContext(DbContextOptions<DocumentationDbContext> options)
    : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ajouter la configuration pour UserPermission
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);
                
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Documentation)
                .WithMany(d => d.UserPermissions)
                .HasForeignKey(up => up.DocumentationId);
            
            modelBuilder.Entity<UserPermission>()
            .HasIndex(up => new { up.UserId, up.DocumentationId })
            .IsUnique();
        }
    }
}
