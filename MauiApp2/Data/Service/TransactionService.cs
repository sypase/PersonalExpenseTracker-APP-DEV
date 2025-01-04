using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiApp2.Data.Models;
using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Globalization;

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
            public Dictionary<string, List<Transaction>> UserTransactions { get; set; }  // Dictionary for each username and its transactions
        }

        // Get a list of transactions for a specific user
        public async Task<List<Transaction>> GetTransactionsAsync(string refUsername)
        {
            try
            {
                var transactionContainer = await Utils.LoadFromJsonAsync<TransactionContainer>(transactionsFilePath);
                if (transactionContainer != null && transactionContainer.UserTransactions.ContainsKey(refUsername))
                {
                    return transactionContainer.UserTransactions[refUsername];
                }
                return new List<Transaction>(); // Return empty list if user not found or no transactions
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting transactions for user '{refUsername}': {ex.Message}");
                return new List<Transaction>(); // Return empty list on error
            }
        }

        // Save transactions for a specific user
        public async Task SaveTransactionsAsync(IEnumerable<Transaction> transactions, string refUsername)
        {
            try
            {
                var transactionContainer = await Utils.LoadFromJsonAsync<TransactionContainer>(transactionsFilePath);
                if (transactionContainer == null)
                {
                    transactionContainer = new TransactionContainer
                    {
                        UserTransactions = new Dictionary<string, List<Transaction>>()
                    };
                }

                // Update the user's transactions or add them if not already present
                if (transactionContainer.UserTransactions.ContainsKey(refUsername))
                {
                    transactionContainer.UserTransactions[refUsername] = transactions.ToList();
                }
                else
                {
                    transactionContainer.UserTransactions.Add(refUsername, transactions.ToList());
                }

                // Serialize to JSON and save to the file asynchronously
                await Utils.SaveToJsonAsync(transactionContainer, transactionsFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving transactions for user '{refUsername}': {ex.Message}");
            }
        }

        // Add a new transaction for a specific user
        public async Task AddTransactionAsync(Transaction transaction, string refUsername)
        {
            var transactions = await GetTransactionsAsync(refUsername);
            transactions.Add(transaction);
            await SaveTransactionsAsync(transactions, refUsername);  // Save with refUsername
        }

        // Update an existing transaction for a specific user
        public async Task UpdateTransactionAsync(Transaction updatedTransaction, string refUsername)
        {
            var transactions = await GetTransactionsAsync(refUsername);
            var transaction = transactions.FirstOrDefault(t => t.Title == updatedTransaction.Title);

            if (transaction != null)
            {
                // Update transaction fields
                transaction.Amount = updatedTransaction.Amount;
                transaction.Date = updatedTransaction.Date;
                transaction.Type = updatedTransaction.Type;
                transaction.Tags = updatedTransaction.Tags;
                transaction.Notes = updatedTransaction.Notes;
                await SaveTransactionsAsync(transactions, refUsername); // Save with refUsername after update
            }
            else
            {
                Console.WriteLine($"Transaction with title '{updatedTransaction.Title}' not found for user '{refUsername}'.");
            }
        }

        // Delete a transaction by its title for a specific user
        public async Task DeleteTransactionAsync(string title, string refUsername)
        {
            var transactions = await GetTransactionsAsync(refUsername);
            var transactionToDelete = transactions.FirstOrDefault(t => t.Title == title);

            if (transactionToDelete != null)
            {
                transactions.Remove(transactionToDelete);
                await SaveTransactionsAsync(transactions, refUsername); // Save with refUsername after delete
            }
            else
            {
                Console.WriteLine($"Transaction with title '{title}' not found for user '{refUsername}'.");
            }
        }

        // Upload transactions from a CSV file
        public async Task UploadTransactionsAsync(Stream fileStream, string refUsername)
        {
            try
            {
                using (var reader = new StreamReader(fileStream))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<Transaction>().ToList();

                    if (records == null || records.Count == 0)
                    {
                        throw new InvalidOperationException("No valid transactions found in the CSV file.");
                    }

                    // Add each record to the user's transaction list
                    foreach (var record in records)
                    {
                        await AddTransactionAsync(record, refUsername);
                    }

                    _logger.LogInformation($"Successfully uploaded {records.Count} transactions for user '{refUsername}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading transactions for user '{refUsername}': {ex.Message}");
                throw new InvalidOperationException("An error occurred while uploading the transactions.");
            }
        }
    }
}
