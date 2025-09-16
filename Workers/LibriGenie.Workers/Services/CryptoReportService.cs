using LibriGenie.Workers.Services.Models;
using Task = System.Threading.Tasks.Task;

namespace LibriGenie.Workers.Services;

public class CryptoReportService : ICryptoReportService
{
    private readonly ICryptoManager _cryptoManager;
    private readonly IMailService _mailService;
    private readonly ILogger<CryptoReportService> _logger;

    // Static field to track daily droppers (symbols that hit new absolute minimums)
    private static Dictionary<string, (decimal PreviousMin, decimal CurrentMin, DateTime TimeStamp, decimal CurrentPrice, decimal AvgPrice)> _dailyDroppers = new();
    private static DateTime _lastDailyReset = DateTime.UtcNow.Date;

    public CryptoReportService(ICryptoManager cryptoManager, IMailService mailService, ILogger<CryptoReportService> logger)
    {
        _cryptoManager = cryptoManager;
        _mailService = mailService;
        _logger = logger;
    }

    private void ResetDailyDroppersIfNeeded()
    {
        var currentDate = DateTime.UtcNow.Date;
        if (currentDate > _lastDailyReset)
        {
            _dailyDroppers.Clear();
            _lastDailyReset = currentDate;
            _logger.LogInformation("Daily droppers reset for new day: {date}", currentDate);
        }
    }

    private void TrackDailyDropper(string symbol, decimal previousMin, decimal currentMin, decimal currentPrice, decimal avgPrice)
    {
        ResetDailyDroppersIfNeeded();
        _dailyDroppers[symbol] = (previousMin, currentMin, DateTime.UtcNow, currentPrice, avgPrice);
        _logger.LogInformation("Tracked daily dropper: {symbol} - Previous: {previousMin:F8}, Current: {currentMin:F8}", 
            symbol, previousMin, currentMin);
    }

    public void TrackDailyDroppers(Dictionary<string, (List<string> events, decimal score)> cryptoEvents, Dictionary<string, CryptoMetrics> cryptoMetrics)
    {
        // Track daily droppers (symbols that hit new absolute minimums)
        foreach (var kvp in cryptoEvents)
        {
            var symbol = kvp.Key;
            var events = kvp.Value.events;
            
            // Check if any event is about new absolute minimum
            var newAbsoluteMinEvent = events.FirstOrDefault(e => e.Contains("NEW ABSOLUTE MIN"));
            if (!string.IsNullOrEmpty(newAbsoluteMinEvent) && cryptoMetrics.ContainsKey(symbol))
            {
                var metrics = cryptoMetrics[symbol];
                TrackDailyDropper(symbol, metrics.PreviousAbsoluteMin, metrics.AbsoluteMin, metrics.CurrentPrice, metrics.AveragePrice);
            }
        }

        // Remove symbols from daily droppers if current price is higher than average minimum
        var symbolsToRemove = new List<string>();
        foreach (var kvp in _dailyDroppers)
        {
            var symbol = kvp.Key;
            if (cryptoMetrics.ContainsKey(symbol))
            {
                var metrics = cryptoMetrics[symbol];
                if (metrics.CurrentPrice > metrics.AverageMin)
                {
                    symbolsToRemove.Add(symbol);
                    _logger.LogInformation("Removed {symbol} from daily droppers - current price {currentPrice:F8} is above average min {avgMin:F8}", 
                        symbol, metrics.CurrentPrice, metrics.AverageMin);
                }
            }
        }

        foreach (var symbol in symbolsToRemove)
        {
            _dailyDroppers.Remove(symbol);
        }
    }

    public async Task ProcessCryptoSpikeTaskAsync(
        Services.Models.Task task, 
        Dictionary<string, (List<string> events, decimal score)> cryptoEvents, 
        Dictionary<string, CryptoMetrics> cryptoMetrics, 
        CancellationToken stoppingToken)
    {
        try
        {
            // Analyze Coinbase positions if credentials are provided
            List<CoinbasePosition> profitablePositions = new List<CoinbasePosition>();
            
            if (!string.IsNullOrEmpty(task.CoinbaseName) && !string.IsNullOrEmpty(task.CoinbasePrivateKey))
            {
                _logger.LogInformation("Processing crypto spike task for user {email} with Coinbase integration - Account: {coinbaseName}", 
                    task.Email, task.CoinbaseName);
               
            }

            // Analyze symbols for short-term investment potential
            var shortTermOpportunities = await _cryptoManager.AnalyzeShortTermInvestmentOpportunities(cryptoMetrics, stoppingToken);
           
            var relevantEvents = new Dictionary<string, (List<string> events, decimal score)>();
            var relevantMetrics = new Dictionary<string, CryptoMetrics>();

            // Check which symbols from the task have events or metrics
            foreach (var symbol in task.Symbols)
            {
                if (cryptoEvents.ContainsKey(symbol))
                {
                    relevantEvents[symbol] = cryptoEvents[symbol];
                }

                if (cryptoMetrics.ContainsKey(symbol))
                {
                    relevantMetrics[symbol] = cryptoMetrics[symbol];
                }
            }

            // Only include symbols that have events in the email
            var symbolsWithEvents = relevantEvents.Keys.ToList();
            
            if (!symbolsWithEvents.Any())
            {
                _logger.LogInformation("No symbols with events for user {email}, skipping email", task.Email);
                return;
            }

            // Filter metrics to only include symbols that have events
            var filteredMetrics = relevantMetrics
                .Where(x => symbolsWithEvents.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Create email content
            var emailBody = "Crypto Metrics Report\n\n";

            // Section 1: Profitable Positions (if Coinbase integration is available)
            if (profitablePositions.Any())
            {
                emailBody += "💰 PROFITABLE POSITIONS READY TO CLOSE 💰\n";
                emailBody += "==========================================\n\n";

                foreach (var position in profitablePositions)
                {
                    emailBody += $"Product: {position.ProductId}\n";
                    emailBody += $"Position ID: {position.PositionId}\n";
                    emailBody += $"Side: {position.Side}\n";
                    emailBody += $"Position Size: {position.PositionSize:F8}\n";
                    emailBody += $"Open Price: {position.OpenPrice:F8}\n";
                    emailBody += $"Unrealized P&L: {position.UnrealizedProfitLoss:F8}\n";
                    emailBody += $"Position Value: {position.PositionValue:F8}\n";
                    emailBody += $"Created: {position.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Updated: {position.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 2: Short-Term Investment Opportunities
            var relevantOpportunities = shortTermOpportunities
                .Where(o => task.Symbols.Contains(o.Symbol))
                .Take(10) // Top 10 opportunities
                .ToList();

            if (relevantOpportunities.Any())
            {
                emailBody += "🚀 DAILY INVESTMENT OPPORTUNITIES (€100 MAX) 🚀\n";
                emailBody += "===============================================\n\n";
                emailBody += "Based on daily price analysis with Coinbase fees (1.5 EUR buy + 1.5 EUR sell + 0.01% of price)\n";
                emailBody += "All opportunities calculated for maximum €100 investment to ensure profitable returns\n\n";

                foreach (var opportunity in relevantOpportunities)
                {
                    emailBody += $"Symbol: {opportunity.Symbol}\n";
                    emailBody += $"Opportunity Type: {opportunity.OpportunityType}\n";
                    emailBody += $"Opportunity Score: {opportunity.OpportunityScore:F2}%\n";
                    emailBody += $"Current Price: {opportunity.CurrentPrice:F8}\n";
                    emailBody += $"Daily Min: {opportunity.AverageMin:F8}\n";
                    emailBody += $"Daily Max: {opportunity.AverageMax:F8}\n";
                    emailBody += $"Daily Range: {opportunity.TwoWeekRange:F8}\n";
                    emailBody += $"Total Fees (Buy+Sell): {opportunity.TotalFees:F8}\n";
                    emailBody += $"Volume: {opportunity.Volume:F2}\n";
                    emailBody += $"Daily Volatility Count: {opportunity.DailyVolatilityCount}\n";
                    emailBody += $"Risk/Reward Ratio: {opportunity.RiskRewardRatio:F4}\n";
                    emailBody += $"Recommendation: {opportunity.Recommendation}\n";
                    
                    // Show profit scenarios
                    if (opportunity.ProfitAtAvgMax > 0)
                    {
                        emailBody += $"  • Buy Current → Sell at Daily Max: {opportunity.ProfitPercentageAtAvgMax:F2}% profit\n";
                    }
                    if (opportunity.ProfitAtCurrentFromMin > 0)
                    {
                        emailBody += $"  • Buy at Daily Min → Sell Current: {opportunity.ProfitPercentageAtCurrentFromMin:F2}% profit\n";
                    }
                    if (opportunity.ProfitAtFullRange > 0)
                    {
                        emailBody += $"  • Buy at Daily Min → Sell at Daily Max: {opportunity.ProfitPercentageAtFullRange:F2}% profit\n";
                    }
                    
                    emailBody += "\n";
                }
            }

            // Section 3: Primary Symbols (symbols marked as primary)
            var primarySymbols = filteredMetrics
                .Where(x => task.PrimarySymbols.Contains(x.Key))
                .OrderByDescending(x => relevantEvents.ContainsKey(x.Key) ? relevantEvents[x.Key].score : 0)
                .ToList();

            if (primarySymbols.Any())
            {
                emailBody += "⭐ PRIMARY SYMBOLS ⭐\n";
                emailBody += "===================\n\n";

                foreach (var kvp in primarySymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events Detected:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 4: The Dropper of the day (symbols that hit new absolute minimums today)
            var dailyDroppersForTask = _dailyDroppers
                .Where(x => task.Symbols.Contains(x.Key))
                .OrderByDescending(x => x.Value.PreviousMin - x.Value.CurrentMin) // Order by biggest drop (previous - current)
                .ToList();

            if (dailyDroppersForTask.Any())
            {
                emailBody += "📉 THE DROPPER OF THE DAY 📉\n";
                emailBody += "============================\n\n";

                foreach (var kvp in dailyDroppersForTask)
                {
                    var symbol = kvp.Key;
                    var dropperInfo = kvp.Value;
                    var dropAmount = dropperInfo.PreviousMin - dropperInfo.CurrentMin;
                    var dropPercentage = (dropAmount / dropperInfo.PreviousMin) * 100;
                    var currentPercentage = (cryptoMetrics[symbol].CurrentPrice / dropperInfo.CurrentMin) * 100;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Current Price: {cryptoMetrics[symbol].CurrentPrice:F8}\n";
                    emailBody += $"Current Percentage: {currentPercentage:F2}%\n";
                    emailBody += $"Average Daily Price: {cryptoMetrics[symbol].AveragePrice:F8}\n";
                    emailBody += $"Previous Absolute Min: {dropperInfo.PreviousMin:F8}\n";
                    emailBody += $"New Absolute Min: {dropperInfo.CurrentMin:F8}\n";
                    emailBody += $"Drop Amount: {dropAmount:F8}\n";
                    emailBody += $"Drop Percentage: {dropPercentage:F2}%\n";
                    emailBody += $"Time of Drop: {dropperInfo.TimeStamp:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 5: Most Volatile Symbols (top 10 by volatility count)
            var mostVolatileSymbols = relevantMetrics
                .Where(x => x.Value.DailyVolatilityCount > 0)
                .OrderByDescending(x => x.Value.DailyVolatilityCount)
                .Take(10)
                .ToList();

            if (mostVolatileSymbols.Any())
            {
                emailBody += "📈 MOST VOLATILE SYMBOLS (Top 10) 📈\n";
                emailBody += "=====================================\n\n";

                foreach (var kvp in mostVolatileSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 6: Biggest Price Change (top 10 by daily price change)
            var biggestPriceChangeSymbols = relevantMetrics
                .Where(x => x.Value.DailyPriceChange > 0)
                .OrderByDescending(x => x.Value.DailyPriceChange)
                .Take(10)
                .ToList();

            if (biggestPriceChangeSymbols.Any())
            {
                emailBody += "💰 BIGGEST PRICE CHANGE (Top 10) 💰\n";
                emailBody += "==================================\n\n";

                foreach (var kvp in biggestPriceChangeSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    emailBody += $"Daily Price Change: {metrics.DailyPriceChange:F8}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            // Section 7: Rest of Symbols (sorted by score)
            var restSymbols = filteredMetrics
                .Where(x => !task.PrimarySymbols.Contains(x.Key) && 
                           !mostVolatileSymbols.Any(v => v.Key == x.Key) && 
                           !biggestPriceChangeSymbols.Any(b => b.Key == x.Key) &&
                           !dailyDroppersForTask.Any(d => d.Key == x.Key))
                .OrderByDescending(x => relevantEvents.ContainsKey(x.Key) ? relevantEvents[x.Key].score : 0)
                .ToList();

            if (restSymbols.Any())
            {
                emailBody += "📊 REMAINING SYMBOLS (Sorted by Score) 📊\n";
                emailBody += "==========================================\n\n";

                foreach (var kvp in restSymbols)
                {
                    var symbol = kvp.Key;
                    var metrics = kvp.Value;
                    var hasEvents = relevantEvents.ContainsKey(symbol);
                    var events = hasEvents ? relevantEvents[symbol].events : new List<string>();
                    var score = hasEvents ? relevantEvents[symbol].score : 0;

                    emailBody += $"Symbol: {symbol}\n";
                    if (hasEvents)
                    {
                        emailBody += $"Score: {score:F2}\n";
                        emailBody += "Events:\n";
                        foreach (var evt in events)
                        {
                            emailBody += $"  • {evt}\n";
                        }
                    }
                    emailBody += $"Current Price: {metrics.CurrentPrice:F8}\n";
                    emailBody += $"Volume: {metrics.Volume:F8}\n";
                    emailBody += $"Daily Average Price: {metrics.AveragePrice:F8} (from {metrics.DailyPriceCount} updates today)\n";
                    emailBody += $"Daily Price Sum: {metrics.DailyPriceSum:F8}\n";
                    emailBody += $"Daily Range: {metrics.DailyMin:F8} - {metrics.DailyMax:F8}\n";
                    emailBody += $"Daily Change: {metrics.DailyPriceChange:F8}\n";
                    emailBody += $"Volatility Count: {metrics.DailyVolatilityCount}\n";
                    emailBody += $"2-Week Average Min: {metrics.AverageMin:F8}\n";
                    emailBody += $"2-Week Average Max: {metrics.AverageMax:F8}\n";
                    emailBody += $"All-Time Absolute Min: {metrics.AbsoluteMin:F8}\n";
                    emailBody += $"All-Time Absolute Max: {metrics.AbsoluteMax:F8}\n";
                    emailBody += $"Stored Below Avg Min Threshold: {(metrics.StoredBelowAvgMinThreshold.HasValue ? metrics.StoredBelowAvgMinThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Stored Above Avg Max Threshold: {(metrics.StoredAboveAvgMaxThreshold.HasValue ? metrics.StoredAboveAvgMaxThreshold.Value.ToString("F8") : "None")}\n";
                    emailBody += $"Last Updated: {metrics.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Price Update: {metrics.LastPriceUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += $"Last Average Update: {metrics.LastAverageUpdate:yyyy-MM-dd HH:mm:ss} UTC\n";
                    emailBody += "\n";
                }
            }

            emailBody += "This report was generated automatically by Libri Genie Crypto Spikes detection system.";

            // Add Coinbase integration info if credentials are provided
            if (!string.IsNullOrEmpty(task.CoinbaseName) || !string.IsNullOrEmpty(task.CoinbasePrivateKey))
            {
                emailBody += "\n\n🔗 COINBASE INTEGRATION\n";
                emailBody += "=====================\n";
                if (!string.IsNullOrEmpty(task.CoinbaseName))
                {
                    emailBody += $"Account: {task.CoinbaseName}\n";
                }
                if (!string.IsNullOrEmpty(task.CoinbasePrivateKey))
                {
                    emailBody += "Status: Enhanced monitoring enabled\n";
                    emailBody += "Features: Real-time portfolio tracking, position analysis, profit alerts\n";
                    if (profitablePositions.Any())
                    {
                        emailBody += $"Profitable Positions Found: {profitablePositions.Count}\n";
                    }
                }
            }

            // Send email
            var subject = "Crypto Spike Alerts & Metrics";
            await _mailService.Send(task.Email, subject, emailBody, stoppingToken);

            _logger.LogInformation("Sent crypto report to {email} for {count} symbols with events (total symbols: {totalSymbols})",
                task.Email, symbolsWithEvents.Count, task.Symbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing crypto spike task for {email}: {message}", task.Email, ex.Message);
        }
    }
}
