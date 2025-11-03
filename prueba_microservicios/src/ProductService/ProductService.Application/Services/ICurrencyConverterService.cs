namespace ProductService.Application.Services;

public interface ICurrencyConverterService
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency);
}

