using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiApp2.Data.Models;

namespace MauiApp2.Data.Service
{
    internal class DashboardService
    {
        private readonly TransactionService _transactionService;
        private readonly DebtService _debtService;

        public DashboardService(TransactionService transactionService, DebtService debtService)
        {
            _transactionService = transactionService;
            _debtService = debtService;
        }

        // Get total number of transactions and total transaction amount (inflows + debts - outflows)
        public async Task<(int totalTransactions, decimal totalTransactionAmount)> GetTotalTransactionsAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var totalTransactions = transactions.Count;
            var totalTransactionAmount = transactions.Sum(t =>
                t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase) ? t.Amount :
                t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase) ? -t.Amount : 0);

            return (totalTransactions, totalTransactionAmount);
        }

        // Get total inflows, outflows, debt, cleared debt, and remaining debt
        public async Task<(decimal totalInflows, decimal totalOutflows, decimal totalDebt, decimal clearedDebt, decimal remainingDebt)> GetTransactionSummaryAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var totalInflows = transactions.Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
            var totalOutflows = transactions.Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);

            var debts = await _debtService.GetDebtsAsync(username);
            var totalDebt = debts.Sum(d => d.AmountOwed);
            var clearedDebt = debts.Where(d => d.IsCleared).Sum(d => d.AmountOwed);
            var remainingDebt = debts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed);

            return (totalInflows, totalOutflows, totalDebt, clearedDebt, remainingDebt);
        }

        // Get highest and lowest inflow, outflow, and debt transactions
        public async Task<(Transaction highestInflow, Transaction lowestInflow, Transaction highestOutflow, Transaction lowestOutflow, Debt highestDebt, Debt lowestDebt)> GetHighestAndLowestTransactionsAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var debts = await _debtService.GetDebtsAsync(username);

            var highestInflow = transactions.Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                                            .OrderByDescending(t => t.Amount).FirstOrDefault();
            var lowestInflow = transactions.Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                                           .OrderBy(t => t.Amount).FirstOrDefault();
            var highestOutflow = transactions.Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                                           .OrderByDescending(t => t.Amount).FirstOrDefault();
            var lowestOutflow = transactions.Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(t => t.Amount).FirstOrDefault();

            var highestDebt = debts.OrderByDescending(d => d.AmountOwed).FirstOrDefault();
            var lowestDebt = debts.OrderBy(d => d.AmountOwed).FirstOrDefault();

            return (highestInflow, lowestInflow, highestOutflow, lowestOutflow, highestDebt, lowestDebt);
        }

        // Get pending debts for the dashboard
        public async Task<List<Debt>> GetPendingDebtsAsync(string username)
        {
            var debts = await _debtService.GetDebtsAsync(username);
            return debts.Where(d => !d.IsCleared && d.DueDate > DateTime.Now).OrderBy(d => d.DueDate).ToList();
        }

        // Filter transactions by date range
        public async Task<List<Transaction>> FilterTransactionsByDateAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            return transactions.Where(t => t.Date >= startDate && t.Date <= endDate).ToList();
        }

        // Get category-wise transaction summary for donut chart
        public async Task<Dictionary<string, decimal>> GetCategoryWiseTransactionSummaryAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var categorySummary = transactions
                .GroupBy(t => t.Type) // Group by 'Credit' and 'Debit'
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            return categorySummary;
        }

        // Get transactions for line chart (grouped by date)
        public async Task<Dictionary<DateTime, decimal>> GetTransactionsForLineChartAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .GroupBy(t => t.Date.Value.Date) // Use .Value to access the non-nullable DateTime
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            return filteredTransactions;
        }
    }
}