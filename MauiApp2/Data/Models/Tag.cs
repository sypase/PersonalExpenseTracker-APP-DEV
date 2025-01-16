using System;
using System.Collections.Generic;

namespace MauiApp2.Data.Models
{
    // Enum for predefined tags
    public enum PredefinedTags
    {
        Yearly,
        Monthly,
        Food,
        Drinks,
        Clothes,
        Gadgets,
        Miscellaneous,
        Fuel,
        Rent,
        EMI,
        Party
    }

    internal class Tag
    {
        public List<string> DefaultTags { get; set; }

        public Tag()
        {
            // Initialize default tags using the PredefinedTags enum
            DefaultTags = Enum.GetNames(typeof(PredefinedTags)).ToList();
        }

        // Add a new tag
        public void AddTag(string tag)
        {
            if (!DefaultTags.Contains(tag))
                DefaultTags.Add(tag);
        }

        // Remove a tag
        public bool RemoveTag(string tag)
        {
            return DefaultTags.Remove(tag);
        }
    }
}