using System;

namespace SmartAI.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}