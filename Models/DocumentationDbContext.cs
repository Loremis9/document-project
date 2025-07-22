using Microsoft.EntityFrameworkCore;

namespace WEBAPI_m1IL_1.Models
{
    public class DocumentationDbContext: DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<Documentation> Documentations { get; set; }
        public DbSet<DocumentationGroup> DocumentationGroups { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<DocumentationFile> DocumentationFiles { get; set; }
        public DocumentationDbContext(DbContextOptions<DocumentationDbContext> options)
    : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Clé composite pour UserGroup
            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            // Clé composite pour DocumentationGroup
            modelBuilder.Entity<DocumentationGroup>()
                .HasKey(dg => new { dg.DocumentationId, dg.GroupId });

            // Clé composite pour Permission
            modelBuilder.Entity<Permission>()
                .HasKey(p => new { p.DocumentationId, p.GroupId });

            // Relations (exemples, à adapter selon tes besoins)
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroup)
                .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);

            modelBuilder.Entity<DocumentationGroup>()
                .HasOne(dg => dg.Documentation)
                .WithMany(d => d.DocumentationGroups)
                .HasForeignKey(dg => dg.DocumentationId);

            modelBuilder.Entity<DocumentationGroup>()
                .HasOne(dg => dg.Group)
                .WithMany(g => g.DocumentationGroups)
                .HasForeignKey(dg => dg.GroupId);
            modelBuilder.Entity<Permission>()
           .HasOne(p => p.DocumentationGroup)
           .WithOne(dg => dg.Permission)
           .HasForeignKey<Permission>(p => new { p.DocumentationId, p.GroupId });

        }
    }
}
