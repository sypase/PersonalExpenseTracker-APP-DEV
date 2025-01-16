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

        public async Task<(decimal totalInflows, decimal totalOutflows, decimal totalDebt, decimal clearedDebt, decimal remainingDebt)> GetTransactionSummaryAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username) ?? new List<Transaction>();
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var debts = await _debtService.GetDebtsAsync(username) ?? new List<Debt>();
            var filteredDebts = debts
                .Where(d => d.DueDate >= startDate && d.DueDate <= endDate)
                .ToList();

            var totalInflows = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            var totalOutflows = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            var totalDebt = filteredDebts.Sum(d => d.AmountOwed);
            var clearedDebt = filteredDebts.Where(d => d.IsCleared).Sum(d => d.AmountOwed);
            var remainingDebt = filteredDebts.Where(d => !d.IsCleared).Sum(d => d.AmountOwed);

            return (totalInflows, totalOutflows, totalDebt, clearedDebt, remainingDebt);
        }

        #endregion

        #region Highest and Lowest Transactions

        public async Task<(Transaction highestInflow, Transaction lowestInflow, Transaction highestOutflow, Transaction lowestOutflow, Debt highestDebt, Debt lowestDebt)> GetHighestAndLowestTransactionsAsync(string username, DateTime startDate, DateTime endDate)
        {
            var transactions = await _transactionService.GetTransactionsAsync(username);
            var filteredTransactions = transactions
                .Where(t => t.Date.HasValue && t.Date.Value >= startDate && t.Date.Value <= endDate)
                .ToList();

            var debts = await _debtService.GetDebtsAsync(username);
            var filteredDebts = debts
                .Where(d => d.DueDate >= startDate && d.DueDate <= endDate)
                .ToList();

            var highestInflow = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            var lowestInflow = filteredTransactions
                .Where(t => t.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Amount)
                .FirstOrDefault();

            var highestOutflow = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            var lowestOutflow = filteredTransactions
                .Where(t => t.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Amount)
                .FirstOrDefault();

            var highestDebt = filteredDebts
                .OrderByDescending(d => d.AmountOwed)
                .FirstOrDefault();

            var lowestDebt = filteredDebts
                .OrderBy(d => d.AmountOwed)
                .FirstOrDefault();

            return (highestInflow, lowestInflow, highestOutflow, lowestOutflow, highestDebt, lowestDebt);
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