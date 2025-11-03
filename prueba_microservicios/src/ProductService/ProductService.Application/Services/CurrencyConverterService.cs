using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProductService.Application.Services;

public class CurrencyConverterService : ICurrencyConverterService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CurrencyConverterService> _logger;
    private const string ExchangeRateApiUrl = "https://api.exchangerate-api.com/v4/latest/";

    public CurrencyConverterService(
        HttpClient httpClient,
        IDistributedCache cache,
        ILogger<CurrencyConverterService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency);
        return amount * rate;
    }

    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var cacheKey = $"exchange_rate_{fromCurrency}_{toCurrency}";
        
        // Try to get from cache
        var cachedRate = await _cache.GetStringAsync(cacheKey);
        if (cachedRate != null && decimal.TryParse(cachedRate, out var cachedValue))
        {
            return cachedValue;
        }

        try
        {
            // Fetch from API
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(
                $"{ExchangeRateApiUrl}{fromCurrency.ToUpper()}");

            if (response?.Rates == null)
                throw new Exception("Failed to fetch exchange rates");

            var rate = response.Rates.GetValueOrDefault(toCurrency.ToUpper(), 0m);
            
            if (rate == 0m)
                throw new Exception($"Exchange rate not found for {fromCurrency} to {toCurrency}");

            // Cache for 1 hour
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            
            await _cache.SetStringAsync(cacheKey, rate.ToString(), options);

            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
            throw;
        }
    }

    private class ExchangeRateResponse
    {
        public string Base { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}

