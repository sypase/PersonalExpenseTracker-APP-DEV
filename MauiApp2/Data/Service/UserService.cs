using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiApp2.Data.Models;

namespace MauiApp2.Data.Service
{
    internal class UserService
    {
        private string usersFilePath = Utils.GetUsersPath();

        // Register a new user (save user data)
        public async Task RegisterUser(User newUser)
        {
            try
            {
                // Load existing users from file (if any)
                var existingUsers = await Utils.LoadFromJsonAsync<List<User>>(usersFilePath) ?? new List<User>();

                // Check if username already exists
                if (existingUsers.Any(u => u.Username == newUser.Username))
                {
                    throw new Exception("Username already exists.");
                }

                // Add the new user to the list and save it back to the file
                existingUsers.Add(newUser);
                await Utils.SaveToJsonAsync(existingUsers, usersFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user: {ex.Message}");
                throw;
            }
        }

        // Retrieve a user from the users.json file
        public async Task<User> GetUserAsync(string username)
        {
            try
            {
                var existingUsers = await Utils.LoadFromJsonAsync<List<User>>(usersFilePath);
                return existingUsers?.FirstOrDefault(u => u.Username == username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user: {ex.Message}");
                return null;
            }
        }

        // Validate user login (compare password)
        public bool ValidateUser(User user, string inputPassword)
        {
            if (user == null || string.IsNullOrEmpty(inputPassword))
            {
                return false;
            }

            return user.ValidatePassword(inputPassword);
        }

        // Update user currency
        public async Task UpdateCurrencyAsync(User user, string newCurrency)
        {
            if (user != null)
            {
                user.UpdateCurrency(newCurrency);
                await SaveUserAsync(user);
            }
        }

        // Save user data to the users.json file
        private async Task SaveUserAsync(User user)
        {
            try
            {
                var existingUsers = await Utils.LoadFromJsonAsync<List<User>>(usersFilePath) ?? new List<User>();
                var userIndex = existingUsers.FindIndex(u => u.Username == user.Username);

                if (userIndex >= 0)
                {
                    existingUsers[userIndex] = user; // Update existing user
                }
                else
                {
                    existingUsers.Add(user); // Add new user
                }

                await Utils.SaveToJsonAsync(existingUsers, usersFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user data: {ex.Message}");
            }
        }
    }
}
