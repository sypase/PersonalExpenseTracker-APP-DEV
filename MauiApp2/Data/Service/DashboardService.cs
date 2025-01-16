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

        #region Transaction Summary Methods

        public async Task<(int totalTransactions, decimal totalTransactionAmount)> GetTotalTransactionsAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username) ?? new List<Transaction>();
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var totalTransactions = filteredTransactions.Count;
            var totalTransactionAmount = filteredTransactions.Sum(t =>
                t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase) ? t.Amount :
                t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase) ? -t.Amount : 0);

            return (totalTransactions, totalTransactionAmount);
        }

        public async Task<(decimal totalInflows, decimal totalOutflows, decimal totalDebt, decimal clearedDebt, decimal remainingDebt, decimal balance)> GetTransactionSummaryAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username) ?? new List<Transaction>();
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var debts = await _debtService.GetDebtsAsync(username) ?? new List<Debt>();
            var filteredDebts = debts
                .Where(d => d.DueDate >= startDate && d.DueDate <= endDate)
                .ToList();

            // Calculate total inflows (credits)
            var totalInflows = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            // Calculate total outflows (debits only, excluding debts)
            var totalOutflows = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            // Calculate total debt
            var totalDebt = filteredDebts.Sum(d => d.AmountOwed);

            // Calculate cleared debt
            var clearedDebt = filteredDebts.Where(d => d.IsCleared).Sum(d => d.AmountOwed);

            // Calculate remaining debt
            var remainingDebt = filteredDebts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed);

            // Calculate balance (inflows - outflows)
            var balance = totalInflows - totalOutflows;

            return (totalInflows, totalOutflows, totalDebt, clearedDebt, remainingDebt, balance);
        }

        #endregion

        #region Highest and Lowest Transactions

        public async Task<(List<Transaction> top5HighestInflows, List<Transaction> top5LowestInflows, List<Transaction> top5HighestOutflows, List<Transaction> top5LowestOutflows, List<Debt> top5HighestDebts)> GetTop5TransactionsAndDebtsAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var debts = await _debtService.GetDebtsAsync(username);
            var filteredDebts = debts
                .Where(d => d.DueDate >= startDate && d.DueDate <= endDate)
                .ToList();

            // Get top 5 highest inflows
            var top5HighestInflows = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Amount)
                .Take(5)
                .ToList();

            // Get top 5 lowest inflows
            var top5LowestInflows = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Amount)
                .Take(5)
                .ToList();

            // Get top 5 highest outflows
            var top5HighestOutflows = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Amount)
                .Take(5)
                .ToList();

            // Get top 5 lowest outflows
            var top5LowestOutflows = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Amount)
                .Take(5)
                .ToList();

            // Get top 5 highest debts
            var top5HighestDebts = filteredDebts
                .OrderByDescending(d => d.AmountOwed)
                .Take(5)
                .ToList();

            return (top5HighestInflows, top5LowestInflows, top5HighestOutflows, top5LowestOutflows, top5HighestDebts);
        }

        #endregion

        #region Debt Management

        public async Task<List<Debt>> GetPendingDebtsAsync(string username, DateTime startDate, DateTime endDate)
        {
            var debts = await _debtService.GetDebtsAsync(username);
            return debts
                .Where(d => !d.IsCleared && d.DueDate >= startDate && d.DueDate <= endDate)
                .OrderBy(d => d.DueDate)
                .ToList();
        }

        #endregion

        #region Chart Data Methods

        public async Task<Dictionary<string, decimal>> GetCategoryWiseTransactionSummaryAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var categorySummary = new Dictionary<string, decimal>
            {
                { "Credit", 0 },
                { "Debit", 0 }
            };

            // Add inflows to "Credit"
            foreach (var transaction in filteredTransactions)
            {
                if (transaction.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    categorySummary["Credit"] += transaction.Amount;
                }
                else if (transaction.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    categorySummary["Debit"] += transaction.Amount;
                }
            }

            return categorySummary;
        }

        public async Task<(List<DateTime> Dates, List<decimal> Amounts)> GetTransactionsForLineChartAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .OrderBy(t => t.Date)
                .GroupBy(t => t.Date.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            var dates = filteredTransactions.Select(t => t.Date).ToList();
            var amounts = filteredTransactions.Select(t => t.Amount).ToList();

            return (dates, amounts);
        }

        #endregion
    }
}