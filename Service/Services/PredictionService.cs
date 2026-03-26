using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    public class PredictionService : IPredictionService
    {
        private readonly IRepository<Budget> _budgetRepository;
        private readonly IRepository<Expense> _expenseRepository;

        public PredictionService(
            IRepository<Budget> budgetRepository,
            IRepository<Expense> expenseRepository)
        {
            _budgetRepository = budgetRepository;
            _expenseRepository = expenseRepository;
        }

        public async Task<int?> PredictRiskDays(int userId)
        {
            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            var totalExpense = expenses.Sum(e => e.TotalAmount);

            var budgetLimit = budgets.Sum(b =>
                (b.Percentage * b.AlertThreshold) / 100m);

            var daysPassed = Math.Max(
                (DateTime.UtcNow - expenses.Min(e => e.TransactionDate)).Days,
                1);

            var dailyRate = totalExpense / daysPassed;

            if (dailyRate == 0)
                return null;

            var remaining = budgetLimit - totalExpense;

            if (remaining <= 0)
                return 0;

            return (int)(remaining / dailyRate);
        }
    }
}