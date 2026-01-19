using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    public class InstanceProperty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InstanceId { get; set; }
        public Instance Instance { get; set; }

        [Required]
        [MaxLength(100)]
        public string PropertyName { get; set; }

        [Required]
        [MaxLength(500)]
        public string PropertyValue { get; set; }

        [MaxLength(500)]
        public string Source { get; set; }
    }
}