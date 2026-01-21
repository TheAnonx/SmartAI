using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    /// <summary>
    /// Registro histórico de todas as mudanças em Facts.
    /// NUNCA deletar histórico.
    /// </summary>
    public class FactHistory
    {
        public int Id { get; set; }

        public int FactId { get; set; }
        public Fact? Fact { get; set; }

        public int Version { get; set; }

        [MaxLength(500)]
        public string? PreviousSubject { get; set; }

        [MaxLength(100)]
        public string? PreviousRelation { get; set; }

        [MaxLength(500)]
        public string? PreviousObject { get; set; }

        public double PreviousConfidence { get; set; }

        public FactStatus PreviousStatus { get; set; }

        [MaxLength(500)]
        public string? NewSubject { get; set; }

        [MaxLength(100)]
        public string? NewRelation { get; set; }

        [MaxLength(500)]
        public string? NewObject { get; set; }

        public double NewConfidence { get; set; }

        public FactStatus NewStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = "system";

        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public ChangeType ChangeType { get; set; }
    }

    public enum ChangeType
    {
        CREATED = 0,
        VALIDATED = 1,
        REJECTED = 2,
        DEPRECATED = 3,
        CONFIDENCE_UPDATED = 4,
        CONTENT_EDITED = 5
    }
}