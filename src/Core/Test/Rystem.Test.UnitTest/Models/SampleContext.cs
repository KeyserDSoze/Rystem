using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Rystem.Test.UnitTest.Models
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

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
