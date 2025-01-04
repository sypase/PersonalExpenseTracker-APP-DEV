﻿using MauiApp2.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.IO;
using MauiApp2.Data.Service;
using System.Linq;

internal class FileUploadService
{
    private readonly ILogger<FileUploadService> _logger;
    private readonly TransactionService _transactionService;

    public FileUploadService(ILogger<FileUploadService> logger, TransactionService transactionService)
    {
        _logger = logger;
        _transactionService = transactionService;
    }

    public async Task<List<Transaction>> ProcessCsvFileAsync(IBrowserFile file, string refUsername)
    {
        if (file == null)
        {
            throw new ArgumentException("No file uploaded");
        }

        string fileExtension = Path.GetExtension(file.Name).ToLower();

        if (fileExtension != ".csv")
        {
            throw new ArgumentException("Unsupported file format. Please upload a CSV file.");
        }

        try
        {
            // Parse the CSV file and return the transactions
            return await ParseCsvFileAsync(file, refUsername);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing CSV file: {ex.Message}. File: {file.Name}");
            throw new InvalidOperationException($"An error occurred while processing the CSV file: {ex.Message}", ex);
        }
    }

    private async Task<List<Transaction>> ParseCsvFileAsync(IBrowserFile file, string refUsername)
    {
        var transactions = new List<Transaction>();

        using (var stream = new MemoryStream())
        {
            await file.OpenReadStream().CopyToAsync(stream);
            stream.Position = 0;

            try
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    bool isHeader = true;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var columns = line.Split(',');

                        if (isHeader)
                        {
                            isHeader = false;
                            continue;
                        }

                        try
                        {
                            var transaction = new Transaction(
                                columns[0].Trim(),                    // Title
                                ParseAmount(columns[1].Trim()),       // Amount
                                ParseDate(columns[2].Trim()),         // Date
                                columns[3].Trim(),                    // Type
                                ParseTags(columns[4].Trim()),         // Tags
                                columns.Length > 5 ? columns[5].Trim() : "",  // Notes
                                refUsername                            // RefUsername
                            );

                            transactions.Add(transaction);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error parsing row: {line}. Exception: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error reading the file: {ex.Message}");
                throw new InvalidOperationException("Failed to upload transactions.", ex);
            }
        }

        return transactions;
    }

    private decimal ParseAmount(string amountText)
    {
        if (string.IsNullOrWhiteSpace(amountText)) return 0m;

        // Remove unwanted characters like $ or commas
        amountText = amountText.Replace("$", "").Replace("€", "").Replace(",", "").Trim();

        if (decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException($"Invalid amount format: {amountText}");
    }

    private DateTime? ParseDate(string dateText)
    {
        if (string.IsNullOrWhiteSpace(dateText)) return null;

        string[] formats = {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "MM-dd-yyyy",
            "dd-MM-yyyy"
        };

        if (DateTime.TryParseExact(dateText.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        // Return null if parsing fails
        throw new FormatException($"Invalid date format: {dateText}");
    }

    private List<string> ParseTags(string tagsText)
    {
        if (string.IsNullOrWhiteSpace(tagsText)) return new List<string>();

        return tagsText.Split(',').Select(tag => tag.Trim()).ToList();
    }
}
