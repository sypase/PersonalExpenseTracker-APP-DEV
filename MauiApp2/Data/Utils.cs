using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp2.Data
{
    public static class Utils
    {
        // Define a folder name for your expense tracker data
        private const string EXPENSETRACKER_FOLDER = "ExpenseTrackerData";

        // Get the path to the folder where all your application data will be stored
        private static string GetAppDirectory()
        {
            // Combine the Desktop path with the ExpenseTrackerData folder
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string appDirectory = Path.Combine(desktopPath, EXPENSETRACKER_FOLDER);

            // Create the directory if it does not exist
            if (!Directory.Exists(appDirectory))
            {
                Directory.CreateDirectory(appDirectory);
            }

            return appDirectory;
        }

        // Get the path for storing users.json
        public static string GetUsersPath() => Path.Combine(GetAppDirectory(), "users.json");

        // Get the path for storing transactions.json
        public static string GetTransactionsPath() => Path.Combine(GetAppDirectory(), "transactions.json");

        // Get the path for storing debts.json
        public static string GetDebtsPath() => Path.Combine(GetAppDirectory(), "debts.json");

        // Check if a string is numeric
        public static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }

        // Save data to a JSON file
        public static async Task SaveToJsonAsync<T>(T data, string path)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to file: {ex.Message}");
            }
        }

        // Load data from a JSON file
        public static async Task<T> LoadFromJsonAsync<T>(string path)
        {
            try   
            {
                if (File.Exists(path))
                {
                    string json = await File.ReadAllTextAsync(path);
                    return JsonSerializer.Deserialize<T>(json);
                }
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading from file: {ex.Message}");
                return default;
            }
        }
    }
}
