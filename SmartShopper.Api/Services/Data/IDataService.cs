using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services.Data;

public interface IDataService
{
    // User operations
    Task<User?> GetUserAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByTelegramIdAsync(string telegramChatId);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId);
    
    // Fridge operations
    Task<List<FridgeItem>> GetFridgeItemsAsync(string userId);
    Task<FridgeItem?> GetFridgeItemAsync(string itemId);
    Task<FridgeItem> AddFridgeItemAsync(FridgeItem item);
    Task<FridgeItem> UpdateFridgeItemAsync(FridgeItem item);
    Task<bool> DeleteFridgeItemAsync(string itemId);
    
    // Shopping list operations
    Task<List<ShoppingList>> GetShoppingListsAsync(string userId);
    Task<ShoppingList?> GetShoppingListAsync(string listId);
    Task<ShoppingList> CreateShoppingListAsync(ShoppingList list);
    Task<ShoppingList> UpdateShoppingListAsync(ShoppingList list);
    Task<bool> DeleteShoppingListAsync(string listId);
}
