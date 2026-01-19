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
            base.OnModelCreating(modelBuilder);

            // Auto-referência de conceitos
            modelBuilder.Entity<Concept>()
                .HasOne(c => c.ParentConcept)
                .WithMany(c => c.SubConcepts)
                .HasForeignKey(c => c.ParentConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            modelBuilder.Entity<Concept>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Instance>()
                .HasIndex(i => i.Name);

            // ONTOLOGIA BASE
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Conceito raiz
            modelBuilder.Entity<Concept>().HasData(
                new Concept { Id = 1, Name = "Coisa", Description = "Tudo que existe" }
            );

            // Categorias principais
            modelBuilder.Entity<Concept>().HasData(
                new Concept { Id = 10, Name = "Pessoa", Description = "Ser humano ou agente consciente", ParentConceptId = 1 },
                new Concept { Id = 20, Name = "Lugar", Description = "Local físico ou conceitual", ParentConceptId = 1 },
                new Concept { Id = 30, Name = "Objeto", Description = "Entidade material inanimada", ParentConceptId = 1 },
                new Concept { Id = 40, Name = "Conceito Abstrato", Description = "Entidade não física", ParentConceptId = 1 },
                new Concept { Id = 50, Name = "Evento", Description = "Ocorrência no tempo", ParentConceptId = 1 }
            );
        }
    }
}
