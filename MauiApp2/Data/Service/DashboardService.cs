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

        // Display total number of transactions and total transactions (inflows + debts - outflows)
        public async Task<(int totalTransactions, decimal totalTransactionAmount)> GetTotalTransactionsAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var totalTransactions = transactions.Count;
            var totalTransactionAmount = transactions.Sum(t =>
                t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase) ? t.Amount :
                t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase) ? -t.Amount : 0);

            return (totalTransactions, totalTransactionAmount);
        }

        // Display total inflows, outflows, debt, cleared debt, remaining debt
        public async Task<(decimal totalInflows, decimal totalOutflows, decimal totalDebt, decimal clearedDebt, decimal remainingDebt)> GetTransactionSummaryAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var totalInflows = transactions.Where(t => t.Type.Equals("credit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
            var totalOutflows = transactions.Where(t => t.Type.Equals("debit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);

            var debts = await _debtService.GetDebtsAsync(username);
            var totalDebt = debts.Sum(d => d.AmountOwed);
            var clearedDebt = debts.Where(d => d.IsCleared).Sum(d => d.AmountOwed);
            var remainingDebt = debts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed);

            return (totalInflows, totalOutflows, totalDebt, clearedDebt, remainingDebt);
        }

        // Display highest and lowest inflow, outflow, and debt transactions
        public async Task<(Transaction highestInflow, Transaction lowestInflow, Transaction highestOutflow, Transaction lowestOutflow, Debt highestDebt, Debt lowestDebt)> GetHighestAndLowestTransactionsAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var debts = await _debtService.GetDebtsAsync(username);

            var highestInflow = transactions.Where(t => t.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
                                            .OrderByDescending(t => t.Amount).FirstOrDefault();
            var lowestInflow = transactions.Where(t => t.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
                                           .OrderBy(t => t.Amount).FirstOrDefault();
            var highestOutflow = transactions.Where(t => t.Type.Equals("debit", StringComparison.OrdinalIgnoreCase))
                                           .OrderByDescending(t => t.Amount).FirstOrDefault();
            var lowestOutflow = transactions.Where(t => t.Type.Equals("debit", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(t => t.Amount).FirstOrDefault();

            var highestDebt = debts.OrderByDescending(d => d.AmountOwed).FirstOrDefault();
            var lowestDebt = debts.OrderBy(d => d.AmountOwed).FirstOrDefault();

            return (highestInflow, lowestInflow, highestOutflow, lowestOutflow, highestDebt, lowestDebt);
        }

        // Properly listing pending debts in the dashboard
        public async Task<List<Debt>> GetPendingDebtsAsync(string username)
        {
            var debts = await _debtService.GetDebtsAsync(username);
            return debts.Where(d => !d.IsCleared && d.DueDate > DateTime.Now).OrderBy(d => d.DueDate).ToList();
        }

        // Dashboard filtering by specific date ranges
        public async Task<List<Transaction>> FilterTransactionsByDateAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            return transactions.Where(t => t.Date >= startDate && t.Date <= endDate).ToList();
        }

        // Basic Donut and Linechart calculations (Categorizing by 'credit' and 'debit' types)
        public async Task<Dictionary<string, decimal>> GetCategoryWiseTransactionSummaryAsync(string username)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var categorySummary = transactions
                .GroupBy(t => t.Type) // Grouping by 'credit' and 'debit' types
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            return categorySummary;
        }

        // For creating charts, such as pie or line charts based on transaction amounts by date
        public async Task<List<Transaction>> GetTransactionsForChartAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            return transactions.Where(t => t.Date >= startDate && t.Date <= endDate).ToList();
        }
    }
}
