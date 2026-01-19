using System;
using System.Collections.Generic;

namespace SmartAI.Models
{
    public class Instance
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ConceptId { get; set; }
        public Concept? Concept { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public double Confidence { get; set; } = 1.0;

        public ICollection<InstanceProperty> Properties { get; set; } = new List<InstanceProperty>();
    }
}