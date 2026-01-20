using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    /// <summary>
    /// Insights de código são SEPARADOS de Facts.
    /// NUNCA viram conhecimento factual universal.
    /// São contextuais e situacionais.
    /// </summary>
    public class CodeInsight
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Language { get; set; } = string.Empty;

        [Required]
        public string Context { get; set; } = string.Empty;

        [Required]
        public string Observation { get; set; } = string.Empty;

        public string? Suggestion { get; set; }

        public string? Justification { get; set; }

        public List<string> TradeOffs { get; set; } = new List<string>();

        public InsightSeverity Severity { get; set; } = InsightSeverity.INFO;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(5000)]
        public string? CodeSnippet { get; set; }

        public int? LineNumber { get; set; }
    }

    public enum InsightSeverity
    {
        INFO = 0,
        WARNING = 1,
        ERROR = 2,
        CRITICAL = 3
    }
}