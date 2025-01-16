using MauiApp2.Data.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Data.Service
{
    internal class FileExportService
    {
        private readonly ILogger<FileExportService> _logger;
        private readonly TransactionService _transactionService;
        private readonly DebtService _debtService;

        public FileExportService(ILogger<FileExportService> logger, TransactionService transactionService, DebtService debtService)
        {
            _logger = logger;
            _transactionService = transactionService;
            _debtService = debtService;
        }

        // Export transactions to CSV
        public async Task<string> ExportTransactionsToCsvAsync(string refUsername)
        {
            try
            {
                // Get transactions for the user (filtered by refUsername)
                var transactions = await _transactionService.GetTransactionsAsync(refUsername);

                if (transactions == null || !transactions.Any())
                {
                    throw new InvalidOperationException("No transactions found to export.");
                }

                // CSV header
                var csvContent = "Title,Amount,Date,Type,Tags,Notes\n";

                // Add transactions to CSV
                foreach (var transaction in transactions)
                {
                    // Format tags as "Tag1,Tag2,Tag3"
                    var formattedTags = transaction.Tags != null && transaction.Tags.Any()
                        ? $"\"{string.Join(",", transaction.Tags)}\""
                        : string.Empty;

                    // Ensure proper separation of columns
                    var line = $"{EscapeCsvField(transaction.Title)},{transaction.Amount},{transaction.Date:yyyy-MM-dd},{EscapeCsvField(transaction.Type)},{formattedTags},{EscapeCsvField(transaction.Notes)}\n";
                    csvContent += line;
                }

                _logger.LogInformation($"Successfully exported {transactions.Count} transactions for user '{refUsername}'.");
                return csvContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting transactions: {ex.Message}");
                throw new InvalidOperationException($"An error occurred while exporting transactions: {ex.Message}", ex);
            }
        }

        // Export debts to CSV
        public async Task<string> ExportDebtsToCsvAsync(string refUsername)
        {
            try
            {
                // Get debts for the user (filtered by refUsername)
                var debts = await _debtService.GetDebtsAsync(refUsername);

                if (debts == null || !debts.Any())
                {
                    throw new InvalidOperationException("No debts found to export.");
                }

                // CSV header
                var csvContent = "AmountOwed,DueDate,Creditor,Status\n";

                // Add debts to CSV
                foreach (var debt in debts)
                {
                    var line = $"{debt.AmountOwed},{debt.DueDate:yyyy-MM-dd},{EscapeCsvField(debt.Creditor)},{(debt.IsCleared ? "Cleared" : "Outstanding")}\n";
                    csvContent += line;
                }

                _logger.LogInformation($"Successfully exported {debts.Count} debts for user '{refUsername}'.");
                return csvContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting debts: {ex.Message}");
                throw new InvalidOperationException($"An error occurred while exporting debts: {ex.Message}", ex);
            }
        }

        // Helper method to escape CSV fields
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // Escape fields containing commas or double quotes
            if (field.Contains(",") || field.Contains("\""))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}