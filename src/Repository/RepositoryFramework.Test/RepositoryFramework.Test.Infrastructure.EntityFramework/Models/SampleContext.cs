using Microsoft.EntityFrameworkCore;

namespace RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal
{
    public partial class SampleContext : DbContext
    {
        public SampleContext()
        {
        }

        public SampleContext(DbContextOptions<SampleContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "Test purpose.")]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.IdGruppo);

                entity.Property(e => e.Nome)
                    .HasMaxLength(40)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Identificativo);

                entity.ToTable("User");

                entity.Property(e => e.Cognome)
                    .HasMaxLength(120)
                    .IsUnicode(false);

                entity.Property(e => e.IndirizzoElettronico)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Nome)
                    .HasMaxLength(140)
                    .IsUnicode(false);

                entity.HasMany(d => d.IdGruppos)
                    .WithMany(p => p.IdentificativoUsers)
                    .UsingEntity<Dictionary<string, object>>(
                        "UserCrossGroup",
                        l => l.HasOne<Group>().WithMany().HasForeignKey("IdGruppo").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_UserCrossGroup_Groups"),
                        r => r.HasOne<User>().WithMany().HasForeignKey("IdentificativoUser").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_UserCrossGroup_User"),
                        j =>
                        {
                            j.HasKey("IdentificativoUser", "IdGruppo");

                            j.ToTable("UserCrossGroup");
                        });
            });

            OnModelCreatingPartial(modelBuilder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "Test purpose.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test purpose.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Test purpose.")]
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
