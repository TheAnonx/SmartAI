using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    /// <summary>
    /// Fact é a unidade fundamental de conhecimento no sistema.
    /// NUNCA nasce como VALIDATED.
    /// NUNCA tem Confidence = 1.0.
    /// </summary>
    public class Fact
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Relation { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Confiança: 0.0 < x < 1.0
        /// NUNCA pode ser exatamente 1.0
        /// </summary>
        [Range(0.0, 0.99999)]
        public double Confidence { get; set; } = 0.0;

        public FactStatus Status { get; set; } = FactStatus.CANDIDATE;

        public int Version { get; set; } = 1;

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ValidatedAt { get; set; }

        public DateTime? DeprecatedAt { get; set; }

        [MaxLength(1000)]
        public string? DeprecationReason { get; set; }

        // Relacionamentos
        public ICollection<FactSource> Sources { get; set; } = new List<FactSource>();
        public ICollection<FactHistory> History { get; set; } = new List<FactHistory>();

        /// <summary>
        /// Valida que a confiança nunca seja 1.0
        /// </summary>
        public void SetConfidence(double value)
        {
            if (value >= 1.0)
                throw new InvalidOperationException(
                    "Confiança não pode ser 1.0. Máximo: 0.99999");

            if (value < 0.0)
                throw new InvalidOperationException(
                    "Confiança não pode ser negativa");

            Confidence = value;
        }
    }

    public enum FactStatus
    {
        CANDIDATE = 0,    // Coletado, aguardando validação
        VALIDATED = 1,    // Aprovado pelo usuário
        REJECTED = 2,     // Rejeitado pelo usuário
        DEPRECATED = 3    // Desaprendido (versionado, não deletado)
    }
}