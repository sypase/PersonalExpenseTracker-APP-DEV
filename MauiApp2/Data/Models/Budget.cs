using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2.Data.Models
{
    internal class Budget
    {
        public string Category { get; set; }
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }

        public Budget(string category, decimal limit)
        {
            Category = category;
            Limit = limit;
            Spent = 0m;
        }

        public void UpdateSpent(decimal amount)
        {
            Spent += amount;
        }

        public bool IsLimitExceeded()
        {
            return Spent > Limit;
        }
    }
}
