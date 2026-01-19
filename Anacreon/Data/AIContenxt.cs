using Microsoft.EntityFrameworkCore;
using SmartAI.Models;

namespace SmartAI.Data
{
    public class AIContext : DbContext
    {
        public DbSet<Concept> Concepts { get; set; }
        public DbSet<Instance> Instances { get; set; }
        public DbSet<ConceptProperty> ConceptProperties { get; set; }
        public DbSet<InstanceProperty> InstanceProperties { get; set; }
        public DbSet<Conversation> Conversations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=smartai.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurar relacionamentos
            modelBuilder.Entity<Concept>()
                .HasOne(c => c.ParentConcept)
                .WithMany()
                .HasForeignKey(c => c.ParentConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Instance>()
                .HasOne(i => i.Concept)
                .WithMany(c => c.Instances)
                .HasForeignKey(i => i.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConceptProperty>()
                .HasOne(cp => cp.Concept)
                .WithMany(c => c.Properties)
                .HasForeignKey(cp => cp.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstanceProperty>()
                .HasOne(ip => ip.Instance)
                .WithMany(i => i.Properties)
                .HasForeignKey(ip => ip.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}