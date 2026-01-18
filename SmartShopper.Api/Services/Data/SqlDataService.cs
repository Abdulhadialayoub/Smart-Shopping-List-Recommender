using Microsoft.EntityFrameworkCore;
using SmartShopper.Api.Data;
using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services.Data;

public class SqlDataService : IDataService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SqlDataService> _logger;

    public SqlDataService(ApplicationDbContext context, ILogger<SqlDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // User operations
    public async Task<User?> GetUserAsync(string userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByTelegramIdAsync(string telegramChatId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.TelegramChatId == telegramChatId);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    // Fridge operations
    public async Task<List<FridgeItem>> GetFridgeItemsAsync(string userId)
    {
        return await _context.FridgeItems
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.AddedDate)
            .ToListAsync();
    }

    public async Task<FridgeItem?> GetFridgeItemAsync(string itemId)
    {
        return await _context.FridgeItems.FindAsync(itemId);
    }

    public async Task<FridgeItem> AddFridgeItemAsync(FridgeItem item)
    {
        _context.FridgeItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<FridgeItem> UpdateFridgeItemAsync(FridgeItem item)
    {
        _context.FridgeItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteFridgeItemAsync(string itemId)
    {
        var item = await _context.FridgeItems.FindAsync(itemId);
        if (item == null) return false;
        
        _context.FridgeItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    // Shopping list operations
    public async Task<List<ShoppingList>> GetShoppingListsAsync(string userId)
    {
        return await _context.ShoppingLists
            .Include(list => list.Items)
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedAt)
            .ToListAsync();
    }

    public async Task<ShoppingList?> GetShoppingListAsync(string listId)
    {
        return await _context.ShoppingLists
            .Include(list => list.Items)
            .FirstOrDefaultAsync(list => list.Id == listId);
    }

    public async Task<ShoppingList> CreateShoppingListAsync(ShoppingList list)
    {
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<ShoppingList> UpdateShoppingListAsync(ShoppingList list)
    {
        list.UpdatedAt = DateTime.UtcNow;
        _context.ShoppingLists.Update(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<bool> DeleteShoppingListAsync(string listId)
    {
        var list = await _context.ShoppingLists.FindAsync(listId);
        if (list == null) return false;
        
        _context.ShoppingLists.Remove(list);
        await _context.SaveChangesAsync();
        return true;
    }
}
