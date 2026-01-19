using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAI.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(5000)]
        public string UserMessage { get; set; }

        [Required]
        [MaxLength(5000)]
        public string AIResponse { get; set; }

        public bool LearnedSomething { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
