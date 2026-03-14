using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;      
        public DateTime StartDate { get; set; }        
        public DateTime EndDate { get; set; }          
        public decimal Budget { get; set; }           

        public List<int> ExpenseIds { get; set; } = new();

        public List<ExpenseDto>? Expenses { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal RemainingBudget { get; set; }
        public double Progress { get; set; }
    }

    public class EventCreateDto
    {
        public string Name { get; set; } = null!;       
        public DateTime StartDate { get; set; }        
        public DateTime EndDate { get; set; }           
        public decimal Budget { get; set; }             
        public List<int>? ExpenseIds { get; set; }      
    }

        public class EventUpdateDto
        {
            public string? Name { get; set; }       
            public DateTime? StartDate { get; set; }        
            public DateTime? EndDate { get; set; }           
            public decimal? Budget { get; set; }             
            public List<int>? ExpenseIds { get; set; }
    }



}
