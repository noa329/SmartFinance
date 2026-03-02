using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Sevices
{
    public class BudgetService: IBudgetService
    {
        private readonly IRepository<Budget> _budgetRepository;
        private readonly IContext _context;
        private readonly IMapper _mapper;

        public BudgetService(IRepository<Budget> budgetRepository, IContext context, IMapper mapper)
        {
            _budgetRepository = budgetRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<BudgetDto>> GetAll()
        {
            var budgets = await _budgetRepository.GetAll();
            return _mapper.Map<List<BudgetDto>>(budgets);
        }

        public async Task<BudgetDto?> GetById(int id)
        {
            var budget = await _budgetRepository.GetById(id);
            if (budget == null)
                throw new KeyNotFoundException("Budget not found");

            return _mapper.Map<BudgetDto>(budget);
        }

        public async Task<BudgetDto> Add(BudgetCreateDto dto)
        {
            var entity = _mapper.Map<Budget>(dto);
            await _budgetRepository.Add(entity);
            await _context.Save();
            return _mapper.Map<BudgetDto>(entity);
        }

        public async Task<BudgetDto> Update(int id, BudgetUpdateDto dto)
        {
            var entity = await _budgetRepository.GetById(id);
            if (entity == null)
                throw new KeyNotFoundException("Budget not found");

            if (dto.CategoryId.HasValue)
                entity.CategoryId = dto.CategoryId.Value;

            if (dto.Percentage.HasValue)
                entity.Percentage = dto.Percentage.Value;

            if (dto.AlertEnabled.HasValue)
                entity.AlertEnabled = dto.AlertEnabled.Value;

            if (dto.AlertThreshold.HasValue)
                entity.AlertThreshold = dto.AlertThreshold.Value;

            await _context.Save();
            return _mapper.Map<BudgetDto>(entity);
        }

        public async Task Delete(int id)
        {
            var entity = await _budgetRepository.GetById(id);
            if (entity == null)
                throw new KeyNotFoundException("Budget not found");

            await _budgetRepository.Delete(id);
            await _context.Save();
        }
    }
}
