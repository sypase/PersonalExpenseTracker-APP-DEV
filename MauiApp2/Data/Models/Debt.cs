using System;

namespace MauiApp2.Data.Models
{
    internal class Debt
    {
        public string Creditor { get; set; } // The creditor name
        public decimal AmountOwed { get; set; } // Amount owed to the creditor
        public DateTime DueDate { get; set; } // Due date of the debt
        public bool IsCleared { get; set; } // Indicates if the debt has been cleared
        public string RefUsername { get; set; }  // Reference Username, used to track which user the debt belongs to

        public Debt(string creditor, decimal amountOwed, DateTime dueDate, string refUsername)
        {
            Creditor = creditor;
            AmountOwed = amountOwed;
            DueDate = dueDate;
            IsCleared = false; // Default to false when created
            RefUsername = refUsername;
        }

        public void ClearDebt()
        {
            IsCleared = true;
        }
    }
}
