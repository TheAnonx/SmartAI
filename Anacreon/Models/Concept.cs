using System;
using System.Collections.Generic;

namespace SmartAI.Models
{
    public class Concept
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentConceptId { get; set; }
        public Concept? ParentConcept { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ConceptProperty> Properties { get; set; } = new List<ConceptProperty>();
        public ICollection<Instance> Instances { get; set; } = new List<Instance>();
    }
}