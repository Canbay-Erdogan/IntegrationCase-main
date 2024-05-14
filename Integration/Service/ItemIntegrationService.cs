using Integration.Backend;
using Integration.Common;
using System.Collections.Concurrent;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    private readonly ConcurrentDictionary<string, Item> _savedItems = new();
    private readonly object _lock = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.

    #region Single Server Scenario
    public ItemIntegrationService()  { }
    public Result SaveItem(string itemContent)
    {
        lock (_savedItems.GetOrAdd(itemContent, new Item()))
        {
            // Check the backend to see if the content is already saved.
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
    }

    public async Task<List<Item>> GetAllItems()
    {
        return await Task.Run(() => ItemIntegrationBackend.GetAllItems());
        //return await Task.FromResult(ItemIntegrationBackend.GetAllItems());
    }
    #endregion

    #region Distributed System Scenario
    private readonly IDatabase _cache;
    public ItemIntegrationService(ConnectionMultiplexer redis)
    {
        _cache = redis.GetDatabase();
    }

    public async Task<Result> SaveItemAsync(string itemContent)
    {
        var existingItem = await _cache.StringGetAsync(itemContent);
        if (!existingItem.IsNull)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        await _cache.StringSetAsync(itemContent, "Saved");

        return new Result(true, $"Item with content {itemContent} saved.");
    }

    public async Task<List<Item>> GetAllItemsAsync()
    {
        var keys = await GetAllKeysAsync();
        var items = new List<Item>();

        foreach (var key in keys)
        {
            var content = await _cache.StringGetAsync(key);
            items.Add(new Item { Id = items.Count + 1, Content = content });
        }

        return items;
    }

    private async Task<List<string>> GetAllKeysAsync()
    {
        var endpoints = _cache.Multiplexer.GetEndPoints();
        var keys = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var server = _cache.Multiplexer.GetServer(endpoint);
            await foreach (var key in server.KeysAsync())
            {
                keys.Add((string)key);
            }
        }

        return keys;
    }
    #endregion
}