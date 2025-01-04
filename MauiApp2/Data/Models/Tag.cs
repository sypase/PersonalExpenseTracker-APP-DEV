using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2.Data.Models
{
    internal class Tag
    {
        public List<string> DefaultTags { get; set; }

        public Tag()
        {
            DefaultTags = new List<string>
            {
                "Yearly", "Monthly", "Food", "Drinks", "Clothes", "Gadgets", "Miscellaneous",
                "Fuel", "Rent", "EMI", "Party"
            };
        }

        public void AddTag(string tag)
        {
            if (!DefaultTags.Contains(tag))
                DefaultTags.Add(tag);
        }

        public bool RemoveTag(string tag)
        {
            return DefaultTags.Remove(tag);
        }
    }
}
