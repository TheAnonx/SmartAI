using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    public class Instance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public int ConceptId { get; set; }
        public Concept Concept { get; set; }

        public ICollection<InstanceProperty> Properties { get; set; } = new List<InstanceProperty>();

        [MaxLength(500)]
        public string Source { get; set; }

        public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
    }
}