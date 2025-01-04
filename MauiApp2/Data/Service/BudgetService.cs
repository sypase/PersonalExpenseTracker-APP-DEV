using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiApp2.Data.Models;

namespace MauiApp2.Data.Service
{
    internal class BudgetService
    {
        private string budgetsFilePath = Utils.GetTransactionsPath();  // Use same path as transactions for now

        // Get a list of budgets from a file
        public async Task<List<Budget>> GetBudgetsAsync()
        {
            try
            {
                return await Utils.LoadFromJsonAsync<List<Budget>>(budgetsFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting budgets: {ex.Message}");
                return new List<Budget>();
            }
        }

        // Save a list of budgets to a file
        public async Task SaveBudgetsAsync(List<Budget> budgets)
        {
            try
            {
                await Utils.SaveToJsonAsync(budgets, budgetsFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving budgets: {ex.Message}");
            }
        }

        // Add a new budget
        public async Task AddBudgetAsync(Budget budget)
        {
            var budgets = await GetBudgetsAsync();
            budgets.Add(budget);
            await SaveBudgetsAsync(budgets);
        }

        // Update an existing budget
        public async Task UpdateBudgetAsync(Budget updatedBudget)
        {
            var budgets = await GetBudgetsAsync();
            var budget = budgets.FirstOrDefault(b => b.Category == updatedBudget.Category);
            if (budget != null)
            {
                budget.Limit = updatedBudget.Limit;
                budget.Spent = updatedBudget.Spent;
                await SaveBudgetsAsync(budgets);
            }
        }

        // Delete a budget
        public async Task DeleteBudgetAsync(string category)
        {
            var budgets = await GetBudgetsAsync();
            var budgetToDelete = budgets.FirstOrDefault(b => b.Category == category);
            if (budgetToDelete != null)
            {
                budgets.Remove(budgetToDelete);
                await SaveBudgetsAsync(budgets);
            }
        }
    }
}
