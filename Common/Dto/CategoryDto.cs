using System;
using System.Collections.Generic;
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
        public string Name { get; set; } = null!;
        public bool IsFixed { get; set; }
    }

    public class CategoryUpdateDto
    {
        public string? Name { get; set; }
        public bool? IsFixed { get; set; }
    }
}
