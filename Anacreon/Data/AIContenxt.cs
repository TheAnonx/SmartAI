using Microsoft.EntityFrameworkCore;
using SmartAI.Models;
using System;

namespace SmartAI.Data
{
    public class AIContext : DbContext
    {
        // Entidades antigas (manter temporariamente)
        public DbSet<Concept> Concepts { get; set; }
        public DbSet<ConceptProperty> ConceptProperties { get; set; }
        public DbSet<Instance> Instances { get; set; }
        public DbSet<InstanceProperty> InstanceProperties { get; set; }
        public DbSet<Conversation> Conversations { get; set; }

        // NOVAS ENTIDADES EPISTÊMICAS
        public DbSet<Fact> Facts { get; set; }
        public DbSet<FactSource> FactSources { get; set; }
        public DbSet<FactHistory> FactHistory { get; set; }
        public DbSet<CodeInsight> CodeInsights { get; set; }
        public DbSet<ValidationSession> ValidationSessions { get; set; }
        public DbSet<FactConflict> FactConflicts { get; set; }

        public AIContext(DbContextOptions<AIContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=smartai.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // CONFIGURAÇÃO DAS NOVAS ENTIDADES
            // ==========================================

            // Fact
            modelBuilder.Entity<Fact>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Relation).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Object).IsRequired().HasMaxLength(500);

                // REGRA CRÍTICA: Confiança sempre < 1.0
                entity.Property(e => e.Confidence)
                      .IsRequired()
                      .HasDefaultValue(0.0);

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasDefaultValue(FactStatus.CANDIDATE);

                entity.Property(e => e.Version)
                      .IsRequired()
                      .HasDefaultValue(1);

                // Índices para performance
                entity.HasIndex(e => e.Subject);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.Subject, e.Relation });
                entity.HasIndex(e => e.Confidence);
            });

            // FactSource
            modelBuilder.Entity<FactSource>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Fact)
                      .WithMany(f => f.Sources)
                      .HasForeignKey(e => e.FactId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Identifier).IsRequired().HasMaxLength(500);
                entity.Property(e => e.TrustWeight).HasDefaultValue(0.5);

                entity.HasIndex(e => e.FactId);
                entity.HasIndex(e => e.Type);
            });

            // FactHistory
            modelBuilder.Entity<FactHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Fact)
                      .WithMany(f => f.History)
                      .HasForeignKey(e => e.FactId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ChangedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Version).IsRequired();

                entity.HasIndex(e => e.FactId);
                entity.HasIndex(e => e.ChangedAt);
            });

            // CodeInsight
            modelBuilder.Entity<CodeInsight>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Language).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Context).IsRequired();
                entity.Property(e => e.Observation).IsRequired();

                entity.HasIndex(e => e.Language);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ValidationSession
            modelBuilder.Entity<ValidationSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Query).IsRequired().HasMaxLength(200);

                entity.HasIndex(e => e.StartedAt);
                entity.HasIndex(e => e.UserId);
            });

            // FactConflict
            modelBuilder.Entity<FactConflict>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.FactA)
                      .WithMany()
                      .HasForeignKey(e => e.FactAId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FactB)
                      .WithMany()
                      .HasForeignKey(e => e.FactBId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Subject);
                entity.HasIndex(e => e.IsResolved);
            });

            // ==========================================
            // CONFIGURAÇÃO DAS ENTIDADES ANTIGAS (manter)
            // ==========================================

            // Concept
            modelBuilder.Entity<Concept>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.ParentConcept)
                      .WithMany()
                      .HasForeignKey(e => e.ParentConceptId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentConceptId);
            });

            // ConceptProperty
            modelBuilder.Entity<ConceptProperty>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Concept)
                      .WithMany(c => c.Properties)
                      .HasForeignKey(e => e.ConceptId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ConceptId);
            });

            // Instance
            modelBuilder.Entity<Instance>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Concept)
                      .WithMany(c => c.Instances)
                      .HasForeignKey(e => e.ConceptId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ConceptId);
            });

            // InstanceProperty
            modelBuilder.Entity<InstanceProperty>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Instance)
                      .WithMany(i => i.Properties)
                      .HasForeignKey(e => e.InstanceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.InstanceId);
            });

            // Conversation
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}