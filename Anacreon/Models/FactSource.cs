using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    /// <summary>
    /// Representa uma fonte de informação.
    /// Fonte NÃO define verdade, apenas contexto.
    /// </summary>
    public class FactSource
    {
        public int Id { get; set; }

        public int FactId { get; set; }
        public Fact? Fact { get; set; }

        public SourceType Type { get; set; }

        [Required]
        [MaxLength(500)]
        public string Identifier { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? URL { get; set; }

        /// <summary>
        /// Peso de confiança da fonte.
        /// APENAS INFORMATIVO - nunca é decisivo.
        /// </summary>
        [Range(0.0, 1.0)]
        public double TrustWeight { get; set; } = 0.5;

        public DateTime CollectedAt { get; set; } = DateTime.Now;

        [MaxLength(5000)]
        public string? RawContent { get; set; }
    }

    public enum SourceType
    {
        USER = 0,
        WEB = 1,
        DOCUMENTATION = 2,
        FORUM = 3,
        CODEBASE = 4,
        ACADEMIC = 5
    }
}