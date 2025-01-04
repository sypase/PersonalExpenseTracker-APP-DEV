using System;
using System.Collections.Generic;

namespace MauiApp2.Data.Models
{
    internal class Transaction
    {
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Date { get; set; }  // Nullable DateTime
        public string Type { get; set; }  // Credit, Debit, or Debt
        public List<string> Tags { get; set; }
        public string Notes { get; set; }
        public string RefUsername { get; set; }  // Reference Username

        public Transaction(string title, decimal amount, DateTime? date, string type, List<string> tags, string notes, string refUsername)
        {
            Title = title;
            Amount = amount;
            Date = date;
            Type = type;
            Tags = tags;
            Notes = notes; 
            RefUsername = refUsername;
        }
    }
}
