using System;

namespace SmartAI.Models
{
    public class ConceptProperty
    {
        public int Id { get; set; }
        public int ConceptId { get; set; }
        public Concept? Concept { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyValue { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}