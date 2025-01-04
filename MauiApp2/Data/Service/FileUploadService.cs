using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MauiApp2.Data.Models;
using MauiApp2.Data.Service;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MauiApp2.Data.Service
{
    internal class FileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly TransactionService _transactionService;

        public FileUploadService(ILogger<FileUploadService> logger, TransactionService transactionService)
        {
            _logger = logger;
            _transactionService = transactionService;
        }

        public async Task ProcessCsvFileAsync(IBrowserFile file, string refUsername)
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
                await ParseAndSaveCsvFileAsync(file, refUsername);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing CSV file: {ex.Message}");
                throw new InvalidOperationException($"An error occurred while processing the CSV file: {ex.Message}");
            }
        }

        private async Task ParseAndSaveCsvFileAsync(IBrowserFile file, string refUsername)
        {
            using (var stream = new MemoryStream())
            {
                await file.OpenReadStream().CopyToAsync(stream);
                stream.Position = 0;

                try
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HeaderValidated = null,
                        MissingFieldFound = null,
                        PrepareHeaderForMatch = header => header.ToString().ToLower()
                    };

                    using (var reader = new StreamReader(stream))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<TransactionMap>();
                        var records = csv.GetRecords<Transaction>().ToList();

                        foreach (var record in records)
                        {
                            await _transactionService.AddTransactionAsync(record, refUsername);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error uploading transactions from CSV file: {ex.Message}");
                    throw new InvalidOperationException("Failed to upload transactions.");
                }
            }
        }
    }

    internal class TransactionMap : ClassMap<Transaction>
    {
        public TransactionMap()
        {
            Map(m => m.Title).Name("title");
            Map(m => m.Amount).Name("amount").TypeConverter<DecimalConverter>();
            Map(m => m.Date).Name("date").TypeConverter<DateTimeConverter>();
            Map(m => m.Type).Name("type");
            Map(m => m.Tags).Name("tags").TypeConverter<TagsConverter>();
        }
    }

    internal class DecimalConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData metadata)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0m;

            text = text.Trim().Replace("$", "").Replace("€", "").Replace(" ", "");

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return 0m;
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData metadata)
        {
            return value?.ToString() ?? "0";
        }
    }

    internal class DateTimeConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData metadata)
        {
            if (string.IsNullOrWhiteSpace(text)) return DateTime.MinValue;

            string[] formats = {
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "dd/MM/yyyy",
                "yyyy/MM/dd",
                "MM-dd-yyyy",
                "dd-MM-yyyy"
            };

            if (DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData metadata)
        {
            return value is DateTime date ? date.ToString("yyyy-MM-dd") : "";
        }
    }

    internal class TagsConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData metadata)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // Remove quotes if present and split by comma
            text = text.Trim('"');
            return text.Trim();
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData metadata)
        {
            return value?.ToString() ?? "";
        }
    }
}