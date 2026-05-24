using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Alert
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = null!;
        public bool IsSent { get; set; } = false;

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        [MaxLength(50)]
        public string? RelatedEntityType { get; set; } 
        public int? RelatedEntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
