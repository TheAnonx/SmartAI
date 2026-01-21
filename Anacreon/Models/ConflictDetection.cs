using System;

namespace SmartAI.Models
{
    /// <summary>
    /// Representa um conflito detectado entre fatos.
    /// O sistema NÃO escolhe automaticamente - apresenta ao usuário.
    /// </summary>
    public class FactConflict
    {
        public int Id { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string Relation { get; set; } = string.Empty;

        public int FactAId { get; set; }
        public Fact? FactA { get; set; }

        public int FactBId { get; set; }
        public Fact? FactB { get; set; }

        public double ConfidenceDifference { get; set; }

        public DateTime DetectedAt { get; set; } = DateTime.Now;

        public bool IsResolved { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public ConflictResolution? Resolution { get; set; }

        public string? ResolutionNotes { get; set; }
    }

    public enum ConflictResolution
    {
        KEEP_FACT_A = 0,
        KEEP_FACT_B = 1,
        KEEP_BOTH = 2,
        DEPRECATE_BOTH = 3,
        CREATE_NEW = 4
    }
}