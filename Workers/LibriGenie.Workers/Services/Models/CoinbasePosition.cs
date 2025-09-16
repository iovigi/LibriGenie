using System.Text.Json.Serialization;

namespace LibriGenie.Workers.Services.Models;

public class CoinbasePosition
{
    [JsonPropertyName("position_id")]
    public string PositionId { get; set; } = string.Empty;
    
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty; // "LONG" or "SHORT"
    
    [JsonPropertyName("open_position_size")]
    public string OpenPositionSize { get; set; } = string.Empty;
    
    [JsonPropertyName("open_position_value")]
    public string OpenPositionValue { get; set; } = string.Empty;
    
    [JsonPropertyName("open_position_price")]
    public string OpenPositionPrice { get; set; } = string.Empty;
    
    [JsonPropertyName("unrealized_pnl")]
    public string UnrealizedPnl { get; set; } = string.Empty;
    
    [JsonPropertyName("realized_pnl")]
    public string RealizedPnl { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    // Calculated properties for profit analysis
    public decimal OpenPrice => decimal.TryParse(OpenPositionPrice, out var price) ? price : 0;
    public decimal PositionSize => decimal.TryParse(OpenPositionSize, out var size) ? size : 0;
    public decimal PositionValue => decimal.TryParse(OpenPositionValue, out var value) ? value : 0;
    public decimal UnrealizedProfitLoss => decimal.TryParse(UnrealizedPnl, out var pnl) ? pnl : 0;
    public decimal RealizedProfitLoss => decimal.TryParse(RealizedPnl, out var pnl) ? pnl : 0;
    
    // Calculated profit percentage (assuming 5% profit target)
    public decimal CalculateProfitPercentage(decimal currentPrice, decimal fees = 0.001m) // Default 0.1% fee per trade
    {
        if (OpenPrice <= 0) return 0;
        
        var totalFees = OpenPrice * fees * 2; // Fees for both open and close
        var profitTarget = OpenPrice + (OpenPrice * 0.05m) + totalFees; // 5% profit + fees
        
        if (currentPrice >= profitTarget)
        {
            var actualProfit = currentPrice - OpenPrice - totalFees;
            return (actualProfit / OpenPrice) * 100;
        }
        
        return 0;
    }
    
    public bool CanCloseForProfit(decimal currentPrice, decimal fees = 0.001m)
    {
        if (OpenPrice <= 0) return false;
        
        var totalFees = OpenPrice * fees * 2; // Fees for both open and close
        var profitTarget = OpenPrice + (OpenPrice * 0.05m) + totalFees; // 5% profit + fees
        
        return currentPrice >= profitTarget;
    }
}

public class CoinbasePositionsResponse
{
    [JsonPropertyName("positions")]
    public List<CoinbasePosition> Positions { get; set; } = new List<CoinbasePosition>();
    
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
    
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}






