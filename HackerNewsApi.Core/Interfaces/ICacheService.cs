namespace HackerNewsApi.Core.Interfaces;

public interface ICacheService
{
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key);

    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiry">How long to keep the value</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiry);

    /// <param name="key">Cache key to remove</param>
    Task RemoveAsync(string key);
}