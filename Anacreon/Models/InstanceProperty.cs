using System;

namespace SmartAI.Models
{
    public class InstanceProperty
    {
        public int Id { get; set; }
        public int InstanceId { get; set; }
        public Instance? Instance { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyValue { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}