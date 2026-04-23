using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class DailyCandle
    {
        public DateTime Date { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public long Volume { get; set; }

        public bool IsLimitUp { get; set; }
    }

    public class IntradaySignal
    {
        public bool BreaksAveragePriceAndFails { get; set; }

        public bool ReclaimsAveragePrice { get; set; }
    }

    public class StockCandidateInput
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Market { get; set; }

        public bool IsSt { get; set; }

        public int ConsecutiveLimitUpCount { get; set; }

        public List<DailyCandle> Candles { get; set; }

        public IntradaySignal Intraday { get; set; }
    }

    public class StockRecommendation
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string PatternType { get; set; }

        public decimal Score { get; set; }

        public decimal Support1 { get; set; }

        public decimal Support2 { get; set; }

        public decimal Resistance { get; set; }

        public string TomorrowCondition { get; set; }

        public decimal StopLoss { get; set; }

        public string TakeProfitTrigger { get; set; }

        public string Reason { get; set; }

        public List<string> WatchItems { get; set; }
    }

    public class IndexPageViewModel
    {
        [Required]
        public string RawJson { get; set; }

        public string ErrorMessage { get; set; }

        public List<StockRecommendation> Recommendations { get; set; }
    }
}
