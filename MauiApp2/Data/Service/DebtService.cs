using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiApp2.Data.Models;

namespace MauiApp2.Data.Service
{
    internal class DebtService
    {
        private string debtsFilePath = Utils.GetDebtsPath();

        // Get a list of debts from the debts.json file for a specific user
        public async Task<List<Debt>> GetDebtsAsync(string username)
        {
            try
            {
                var allDebts = await Utils.LoadFromJsonAsync<Dictionary<string, List<Debt>>>(debtsFilePath);
                if (allDebts != null && allDebts.ContainsKey(username))
                {
                    return allDebts[username];
                }
                return new List<Debt>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting debts: {ex.Message}");
                return new List<Debt>();
            }
        }

        // Get total amount of active debts for a user
        public async Task<decimal> GetTotalDebtAsync(string username)
        {
            var debts = await GetDebtsAsync(username);
            return debts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed);
        }

        // Get number of overdue debts
        public async Task<int> GetOverdueDebtsCountAsync(string username)
        {
            var debts = await GetDebtsAsync(username);
            return debts.Count(d => !d.IsCleared && d.DueDate < DateTime.Now);
        }

        // Get total amount of overdue debts
        public async Task<decimal> GetOverdueAmountAsync(string username)
        {
            var debts = await GetDebtsAsync(username);
            return debts.Where(d => !d.IsCleared && d.DueDate < DateTime.Now)
                       .Sum(d => d.AmountOwed);
        }

        // Get debts by creditor
        public async Task<List<Debt>> GetDebtsByCreditorAsync(string username, string creditor)
        {
            var debts = await GetDebtsAsync(username);
            return debts.Where(d => d.Creditor.Equals(creditor, StringComparison.OrdinalIgnoreCase))
                       .ToList();
        }

        // Get debts due within specified days
        public async Task<List<Debt>> GetUpcomingDebtsAsync(string username, int daysAhead)
        {
            var debts = await GetDebtsAsync(username);
            var futureDate = DateTime.Now.AddDays(daysAhead);
            return debts.Where(d => !d.IsCleared &&
                                  d.DueDate > DateTime.Now &&
                                  d.DueDate <= futureDate)
                       .OrderBy(d => d.DueDate)
                       .ToList();
        }

        // Get debt summary
        public async Task<DebtSummary> GetDebtSummaryAsync(string username)
        {
            var debts = await GetDebtsAsync(username);
            return new DebtSummary
            {
                TotalDebts = debts.Count,
                ActiveDebts = debts.Count(d => !d.IsCleared),
                TotalAmount = debts.Sum(d => d.AmountOwed),
                ActiveAmount = debts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed),
                OverdueDebts = debts.Count(d => !d.IsCleared && d.DueDate < DateTime.Now),
                OverdueAmount = debts.Where(d => !d.IsCleared && d.DueDate < DateTime.Now)
                                   .Sum(d => d.AmountOwed),
                EarliestDueDate = debts.Where(d => !d.IsCleared)
                                     .Min(d => d.DueDate),
                LatestDueDate = debts.Where(d => !d.IsCleared)
                                   .Max(d => d.DueDate)
            };
        }

        // Save a list of debts to the debts.json file for all users
        public async Task SaveDebtsAsync(Dictionary<string, List<Debt>> allDebts)
        {
            try
            {
                await Utils.SaveToJsonAsync(allDebts, debtsFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving debts: {ex.Message}");
            }
        }

        // Add a new debt for a specific user
        public async Task AddDebtAsync(Debt debt)
        {
            var allDebts = await Utils.LoadFromJsonAsync<Dictionary<string, List<Debt>>>(debtsFilePath);
            if (allDebts == null)
            {
                allDebts = new Dictionary<string, List<Debt>>();
            }
            if (!allDebts.ContainsKey(debt.RefUsername))
            {
                allDebts[debt.RefUsername] = new List<Debt>();
            }
            allDebts[debt.RefUsername].Add(debt);
            await SaveDebtsAsync(allDebts);
        }

        // Update existing debt
        public async Task UpdateDebtAsync(string username, string creditor, Debt updatedDebt)
        {
            var allDebts = await Utils.LoadFromJsonAsync<Dictionary<string, List<Debt>>>(debtsFilePath);
            if (allDebts != null && allDebts.ContainsKey(username))
            {
                var debtList = allDebts[username];
                var existingDebt = debtList.FirstOrDefault(d => d.Creditor == creditor);
                if (existingDebt != null)
                {
                    debtList.Remove(existingDebt);
                    debtList.Add(updatedDebt);
                    await SaveDebtsAsync(allDebts);
                }
            }
        }

        // Mark a debt as cleared for a specific user
        public async Task ClearDebtAsync(string creditor, string username)
        {
            var allDebts = await Utils.LoadFromJsonAsync<Dictionary<string, List<Debt>>>(debtsFilePath);
            if (allDebts != null && allDebts.ContainsKey(username))
            {
                var debts = allDebts[username];
                var debt = debts.FirstOrDefault(d => d.Creditor == creditor);
                if (debt != null)
                {
                    debt.ClearDebt();
                    await SaveDebtsAsync(allDebts);
                }
            }
        }

        // Delete a debt
        public async Task DeleteDebtAsync(string username, string creditor)
        {
            var allDebts = await Utils.LoadFromJsonAsync<Dictionary<string, List<Debt>>>(debtsFilePath);
            if (allDebts != null && allDebts.ContainsKey(username))
            {
                var debtList = allDebts[username];
                var debtToRemove = debtList.FirstOrDefault(d => d.Creditor == creditor);
                if (debtToRemove != null)
                {
                    debtList.Remove(debtToRemove);
                    await SaveDebtsAsync(allDebts);
                }
            }
        }
    }

    // Additional class to hold debt summary information
    public class DebtSummary
    {
        public int TotalDebts { get; set; }
        public int ActiveDebts { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ActiveAmount { get; set; }
        public int OverdueDebts { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime EarliestDueDate { get; set; }
        public DateTime LatestDueDate { get; set; }
    }
}
