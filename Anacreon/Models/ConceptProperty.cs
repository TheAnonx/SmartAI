using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    public class ConceptProperty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConceptId { get; set; }
        public Concept Concept { get; set; }

        [Required]
        [MaxLength(100)]
        public string PropertyName { get; set; }

        [Required]
        [MaxLength(500)]
        public string PropertyValue { get; set; }

        public bool IsInheritable { get; set; } = true;

        [MaxLength(500)]
        public string Source { get; set; }
    }
}