using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MauiApp2.Data.Models;

namespace MauiApp2.Data.Service
{
    internal class TransactionService
    {
        private readonly string transactionsFilePath = Utils.GetTransactionsPath();
        private readonly ILogger<TransactionService> _logger;

        // Constructor to inject logger
        public TransactionService(ILogger<TransactionService> logger)
        {
            _logger = logger;
        }

        // Class to represent the structure of the transactions file with multiple users
        private class TransactionContainer
        {
            public Dictionary<string, List<Transaction>> UserTransactions { get; set; } = new();
        }

        // Ensure the transactions file is initialized
        private async Task<TransactionContainer> EnsureTransactionContainerAsync()
        {
            var container = await Utils.LoadFromJsonAsync<TransactionContainer>(transactionsFilePath);
            if (container == null)
            {
                container = new TransactionContainer();
                await Utils.SaveToJsonAsync(container, transactionsFilePath);
            }
            return container;
        }

        // Get a list of transactions for a specific user
        public async Task<List<Transaction>> GetTransactionsAsync(string refUsername)
        {
            try
            {
                var transactionContainer = await EnsureTransactionContainerAsync();
                if (transactionContainer.UserTransactions.TryGetValue(refUsername, out var transactions))
                {
                    return transactions;
                }
                return new List<Transaction>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transactions for user '{refUsername}'");
                return new List<Transaction>();
            }
        }

        // Save transactions for a specific user
        public async Task SaveTransactionsAsync(IEnumerable<Transaction> transactions, string refUsername)
        {
            try
            {
                var transactionContainer = await EnsureTransactionContainerAsync();
                transactionContainer.UserTransactions[refUsername] = transactions.ToList();
                await Utils.SaveToJsonAsync(transactionContainer, transactionsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving transactions for user '{refUsername}'");
            }
        }

        // Add a new transaction for a specific user
        public async Task AddTransactionAsync(Transaction transaction, string refUsername)
        {
            try
            {
                var transactions = await GetTransactionsAsync(refUsername);
                transactions.Add(transaction);
                await SaveTransactionsAsync(transactions, refUsername);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding transaction for user '{refUsername}'");
            }
        }

        // Update an existing transaction for a specific user
        public async Task UpdateTransactionAsync(Transaction updatedTransaction, string refUsername)
        {
            try
            {
                var transactions = await GetTransactionsAsync(refUsername);
                var transaction = transactions.FirstOrDefault(t => t.Title == updatedTransaction.Title);

                if (transaction != null)
                {
                    transaction.Amount = updatedTransaction.Amount;
                    transaction.Date = updatedTransaction.Date;
                    transaction.Type = updatedTransaction.Type;
                    transaction.Tags = updatedTransaction.Tags;
                    transaction.Notes = updatedTransaction.Notes;
                    await SaveTransactionsAsync(transactions, refUsername);
                }
                else
                {
                    _logger.LogWarning($"Transaction with title '{updatedTransaction.Title}' not found for user '{refUsername}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating transaction for user '{refUsername}'");
            }
        }

        // Delete a transaction by its title for a specific user
        public async Task DeleteTransactionAsync(string title, string refUsername)
        {
            try
            {
                var transactions = await GetTransactionsAsync(refUsername);
                var transactionToDelete = transactions.FirstOrDefault(t => t.Title == title);

                if (transactionToDelete != null)
                {
                    transactions.Remove(transactionToDelete);
                    await SaveTransactionsAsync(transactions, refUsername);
                }
                else
                {
                    _logger.LogWarning($"Transaction with title '{title}' not found for user '{refUsername}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting transaction for user '{refUsername}'");
            }
        }

        // Upload transactions from a CSV file
        public async Task UploadTransactionsAsync(Stream fileStream, string refUsername)
        {
            try
            {
                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                var records = csv.GetRecords<Transaction>().ToList();

                if (records.Any())
                {
                    foreach (var record in records)
                    {
                        await AddTransactionAsync(record, refUsername);
                    }

                    _logger.LogInformation($"Successfully uploaded {records.Count} transactions for user '{refUsername}'.");
                }
                else
                {
                    _logger.LogWarning("No valid transactions found in the CSV file.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading transactions for user '{refUsername}'");
            }
        }

        // Search, filter, and sort transactions
        public async Task<List<Transaction>> SearchFilterAndSortTransactionsAsync(
            string refUsername,
            string searchQuery = null,
            string typeFilter = null,
            List<string> tagFilters = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortBy = "Date",
            bool ascending = true)
        {
            try
            {
                // Get all transactions for the user
                var transactions = await GetTransactionsAsync(refUsername);

                // Apply search filter (by title or notes)
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    transactions = transactions
                        .Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                    t.Notes.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Apply type filter
                if (!string.IsNullOrEmpty(typeFilter))
                {
                    transactions = transactions
                        .Where(t => t.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Apply tag filters
                if (tagFilters != null && tagFilters.Any())
                {
                    transactions = transactions
                        .Where(t => t.Tags != null && t.Tags.Any(tag => tagFilters.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Apply date range filter
                if (startDate.HasValue && endDate.HasValue)
                {
                    transactions = transactions
                        .Where(t => t.Date >= startDate.Value && t.Date <= endDate.Value)
                        .ToList();
                }

                // Apply sorting
                switch (sortBy.ToLower())
                {
                    case "title":
                        transactions = ascending
                            ? transactions.OrderBy(t => t.Title).ToList()
                            : transactions.OrderByDescending(t => t.Title).ToList();
                        break;
                    case "amount":
                        transactions = ascending
                            ? transactions.OrderBy(t => t.Amount).ToList()
                            : transactions.OrderByDescending(t => t.Amount).ToList();
                        break;
                    case "date":
                    default:
                        transactions = ascending
                            ? transactions.OrderBy(t => t.Date).ToList()
                            : transactions.OrderByDescending(t => t.Date).ToList();
                        break;
                }

                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching, filtering, or sorting transactions for user '{refUsername}'");
                return new List<Transaction>();
            }
        }
    }
}