namespace LibriGenie.Workers.Services.Models;

public class ShortTermInvestmentOpportunity
{
    public string Symbol { get; set; } = string.Empty;
    public string OpportunityType { get; set; } = string.Empty; // BUY_CURRENT_SELL_MAX, BUY_MIN_SELL_CURRENT, BUY_MIN_SELL_MAX
    public decimal CurrentPrice { get; set; }
    public decimal AverageMin { get; set; }
    public decimal AverageMax { get; set; }
    public decimal TwoWeekRange { get; set; }
    public decimal TotalFees { get; set; }
    
    // Profit scenarios
    public decimal ProfitAtAvgMax { get; set; }
    public decimal ProfitPercentageAtAvgMax { get; set; }
    public decimal ProfitAtCurrentFromMin { get; set; }
    public decimal ProfitPercentageAtCurrentFromMin { get; set; }
    public decimal ProfitAtFullRange { get; set; }
    public decimal ProfitPercentageAtFullRange { get; set; }
    
    // Risk metrics
    public decimal RiskRewardRatio { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public decimal OpportunityScore { get; set; }
    
    // Additional metrics
    public double Volume { get; set; }
    public int DailyVolatilityCount { get; set; }
}
