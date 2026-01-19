using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    public class Concept
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? ParentConceptId { get; set; }
        public Concept ParentConcept { get; set; }

        public ICollection<Concept> SubConcepts { get; set; } = new List<Concept>();
        public ICollection<Instance> Instances { get; set; } = new List<Instance>();
        public ICollection<ConceptProperty> Properties { get; set; } = new List<ConceptProperty>();
    }
}