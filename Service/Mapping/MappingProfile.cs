using Common.Dto;
using Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AutoMapper;


namespace Service.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ========================================
            // USER MAPPINGS
            // ========================================

            // Entity -> Dto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt ?? DateTime.MinValue));

            // CreateDto -> Entity
            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Will be set separately
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Repository.Entities.Enums.UserRole.User))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Expenses, opt => opt.Ignore())
                .ForMember(dest => dest.Events, opt => opt.Ignore())
                .ForMember(dest => dest.Budgets, opt => opt.Ignore())
                .ForMember(dest => dest.Alerts, opt => opt.Ignore());

            // UpdateDto -> Entity (only update non-null fields)
            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.FullName, opt => opt.Condition(src => src.Name != null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => src.Phone != null))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.Condition(src => src.Email != null))
            .ForMember(dest => dest.MonthlyIncome, opt =>
            {
                opt.Condition(src => src.MonthlyIncome != null || src.YearlyIncome != null);

                opt.MapFrom(src =>
                    src.MonthlyIncome ??
                    (src.YearlyIncome.HasValue ? src.YearlyIncome.Value / 12 : (decimal?)null)
                );
            });

            // ========================================
            // EXPENSE MAPPINGS
            // ========================================

            // Entity -> Dto
            CreateMap<Expense, ExpenseDto>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.MerchantName ?? ""))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.IsRecurring, opt => opt.MapFrom(src => src.IsRecurring))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.DayOfMonth, opt => opt.MapFrom(src => src.DayOfMonth));

            // CreateDto -> Entity
            CreateMap<createExpenseDto, Expense>()
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.IsRecurring, opt => opt.MapFrom(src => src.IsRecurring))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.DayOfMonth, opt => opt.MapFrom(src => src.DayOfMonth))
                .ForMember(dest => dest.InstallmentsCount, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.InstallmentAmount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.FirstInstallmentDate, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => "Manual"))
                .ForMember(dest => dest.OriginalText, opt => opt.MapFrom(src => ""))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore());

            // UpdateDto -> Entity
            CreateMap<updateExpenseDto, Expense>()
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.IsRecurring, opt => opt.MapFrom(src => src.IsRecurring))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.DayOfMonth, opt => opt.MapFrom(src => src.DayOfMonth));

            // ========================================
            // BUDGET MAPPINGS
            // ========================================

            // Entity -> Dto
            CreateMap<Budget, BudgetDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.CalculatedAmount, opt => opt.MapFrom(src => src.CalculatedAmount));

            // CreateDto -> Entity
            CreateMap<BudgetCreateDto, Budget>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            // UpdateDto -> Entity
            CreateMap<BudgetUpdateDto, Budget>()
                .ForMember(dest => dest.CategoryId, opt => opt.Condition(src => src.CategoryId != null))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId!.Value))
                .ForMember(dest => dest.Percentage, opt => opt.Condition(src => src.Percentage != null))
                .ForMember(dest => dest.Percentage, opt => opt.MapFrom(src => src.Percentage!.Value))
                .ForMember(dest => dest.AlertEnabled, opt => opt.Condition(src => src.AlertEnabled != null))
                .ForMember(dest => dest.AlertEnabled, opt => opt.MapFrom(src => src.AlertEnabled!.Value))
                .ForMember(dest => dest.AlertThreshold, opt => opt.Condition(src => src.AlertThreshold != null))
                .ForMember(dest => dest.AlertThreshold, opt => opt.MapFrom(src => src.AlertThreshold!.Value))
                .ForMember(dest => dest.CalculatedAmount, opt => opt.MapFrom(src => src.CalculatedAmount));
            // ========================================
            // CATEGORY MAPPINGS
            // ========================================

            // Entity -> Dto
            CreateMap<Category, CategoryDto>();

            // CreateDto -> Entity
            CreateMap<CategoryCreateDto, Category>()
                .ForMember(dest => dest.IsSystem, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Expenses, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Budgets, opt => opt.Ignore());

            // UpdateDto -> Entity
            CreateMap<CategoryUpdateDto, Category>()
                .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.IsFixed, opt => opt.Condition(src => src.IsFixed != null))
                .ForMember(dest => dest.IsFixed, opt => opt.MapFrom(src => src.IsFixed!.Value));

            // ========================================
            // EVENT MAPPINGS
            // ========================================

            // Entity -> Dto (with expenses)
            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.ExpenseIds, opt => opt.MapFrom(src => src.Expenses.Select(e => e.Id).ToList()))
                .ForMember(dest => dest.Expenses, opt => opt.MapFrom(src => src.Expenses));

            // CreateDto -> Entity
            CreateMap<EventCreateDto, Event>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Expenses, opt => opt.Ignore());

            // UpdateDto -> Entity
            CreateMap<EventUpdateDto, Event>()
                .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
                .ForMember(dest => dest.StartDate, opt => opt.Condition(src => src.StartDate != null))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate!.Value))
                .ForMember(dest => dest.EndDate, opt => opt.Condition(src => src.EndDate != null))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate!.Value))
                .ForMember(dest => dest.Budget, opt => opt.Condition(src => src.Budget != null))
                .ForMember(dest => dest.Budget, opt => opt.MapFrom(src => src.Budget!.Value));

            // ========================================
            // ALERT MAPPINGS
            // ========================================

            // Entity -> Dto
            CreateMap<Alert, AlertDto>();

            // CreateDto -> Entity
            CreateMap<AlertCreateDto, Alert>()
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ReadAt, opt => opt.Ignore());

            // UpdateDto -> Entity
            CreateMap<AlertUpdateDto, Alert>()
                .ForMember(dest => dest.IsRead, opt => opt.Condition(src => src.IsRead != null))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead!.Value))
                .ForMember(dest => dest.ReadAt, opt => opt.Condition(src => src.ReadAt != null));
               
        }
    }
}