using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    /// <summary>
    /// Representa uma sessão de validação humana.
    /// Rastreia todas as decisões do usuário.
    /// </summary>
    public class ValidationSession
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Query { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        public int CandidatesPresented { get; set; }

        public int FactsApproved { get; set; }

        public int FactsRejected { get; set; }

        public int FactsEdited { get; set; }

        public bool WasCompleted { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        // Relacionamentos
        public ICollection<Fact> RelatedFacts { get; set; } = new List<Fact>();
    }
}