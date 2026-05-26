using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsFixed { get; set; }
    }

    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Name הוא שדה חובה")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string Name { get; set; } = null!;
        public bool IsFixed { get; set; }
    }

    public class CategoryUpdateDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string? Name { get; set; }
        public bool? IsFixed { get; set; }
    }
}
