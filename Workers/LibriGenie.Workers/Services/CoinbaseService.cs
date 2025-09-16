using Jose;
using LibriGenie.Workers.Services.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace LibriGenie.Workers.Services;

public class CoinbaseService : ICoinbaseService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinbaseService> _logger;

    public CoinbaseService(HttpClient httpClient, ILogger<CoinbaseService> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LibriGenie/1.0");
        _logger = logger;
    }

    public string GetToken(string name, string cbPrivateKey, string uri)
    {
        string secret = parseKey(cbPrivateKey);
        var privateKeyBytes = Convert.FromBase64String(secret); // Assuming PEM is base64 encoded
        using var key = ECDsa.Create();
        key.ImportECPrivateKey(privateKeyBytes, out _);

        var payload = new Dictionary<string, object>
             {
                 { "sub", name },
                 { "iss", "coinbase-cloud" },
                 { "nbf", Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "exp", Convert.ToInt64((DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "uri", uri }
             };

        var extraHeaders = new Dictionary<string, object>
             {
                 { "kid", name },
                 // add nonce to prevent replay attacks with a random 10 digit number
                 { "nonce", randomHex(10) },
                 { "typ", "JWT"}
             };

        var encodedToken = JWT.Encode(payload, key, JwsAlgorithm.ES256, extraHeaders);

        // print token
        Console.WriteLine(encodedToken);
        return encodedToken;
    }

    public async Task<List<CoinbasePosition>> GetOpenPositions(string coinbaseName, string coinbasePrivateKey, CancellationToken cancellationToken)
    {
        try
        {
            var allPositions = new List<CoinbasePosition>();
            var endpoint = "api.exchange.coinbase.com/api/v3/brokerage/accounts/positions";

            // Get authentication token
            var token = GetToken(coinbaseName, coinbasePrivateKey, endpoint);

            // Create HTTP request
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync($"https://{endpoint}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get positions from Coinbase API. Status: {status}, Response: {response}", 
                    response.StatusCode, errorContent);
                return new List<CoinbasePosition>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var positionsResponse = JsonConvert.DeserializeObject<CoinbasePositionsResponse>(content);

            if (positionsResponse?.Positions != null)
            {
                allPositions.AddRange(positionsResponse.Positions);
            }

            // Handle pagination if needed
            while (positionsResponse?.HasNext == true && !string.IsNullOrEmpty(positionsResponse.Cursor))
            {
                var nextToken = GetToken(coinbaseName, coinbasePrivateKey, endpoint);

                using var nextRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{endpoint}?cursor={positionsResponse.Cursor}");
                nextRequest.Headers.Add("Authorization", $"Bearer {nextToken}");
                nextRequest.Headers.Add("CB-ACCESS-KEY", coinbaseName);

                var nextResponse = await _httpClient.SendAsync(nextRequest, cancellationToken);
                
                if (!nextResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get next page of positions from Coinbase API. Status: {status}", 
                        nextResponse.StatusCode);
                    break;
                }

                var nextContent = await nextResponse.Content.ReadAsStringAsync(cancellationToken);
                positionsResponse = JsonConvert.DeserializeObject<CoinbasePositionsResponse>(nextContent);

                if (positionsResponse?.Positions != null)
                {
                    allPositions.AddRange(positionsResponse.Positions);
                }
            }

            _logger.LogInformation("Retrieved {count} positions from Coinbase API", allPositions.Count);
            return allPositions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions from Coinbase API: {message}", ex.Message);
            return new List<CoinbasePosition>();
        }
    }

    public async Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.exchange.coinbase.com/products/{symbol}/ticker", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get current price for {symbol}. Status: {status}", symbol, response.StatusCode);
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var ticker = JsonConvert.DeserializeObject<CoinbaseTicker>(content);

            if (ticker != null && decimal.TryParse(ticker.Price, out var price))
            {
                return price;
            }

            _logger.LogWarning("Could not parse price for {symbol}", symbol);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current price for {symbol}: {message}", symbol, ex.Message);
            return 0;
        }
    }


    static Random random = new Random();

    static string randomHex(int digits)
    {
        byte[] buffer = new byte[digits / 2];
        random.NextBytes(buffer);
        string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
        if (digits % 2 == 0)
            return result;
        return result + random.Next(16).ToString("X");
    }

    static string parseKey(string key)
    {
        List<string> keyLines = new List<string>();
        keyLines.AddRange(key.Split("\\n", StringSplitOptions.RemoveEmptyEntries));

        keyLines.RemoveAt(0);
        keyLines.RemoveAt(keyLines.Count - 1);

        return String.Join("", keyLines);
    }
}