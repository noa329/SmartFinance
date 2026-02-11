using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Budget
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        [Range(1, 100, ErrorMessage = "Percentage must be between 1 and 100")]
        public int Percentage { get; set; } 
        public bool AlertEnabled { get; set; } = true;
        public int AlertThreshold { get; set; } = 80;
    }
}
