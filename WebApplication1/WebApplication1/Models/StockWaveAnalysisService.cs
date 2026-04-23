using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication1.Models
{
    public class StockWaveAnalysisService
    {
        public IReadOnlyList<StockRecommendation> Analyze(IEnumerable<StockCandidateInput> candidates)
        {
            if (candidates == null)
            {
                return new List<StockRecommendation>();
            }

            return candidates
                .Where(IsEligible)
                .Select(AnalyzeSingle)
                .Where(result => result != null)
                .OrderByDescending(result => result.Score)
                .ThenByDescending(result => result.Support1)
                .Take(2)
                .ToList();
        }

        private StockRecommendation AnalyzeSingle(StockCandidateInput candidate)
        {
            var candles = (candidate.Candles ?? new List<DailyCandle>())
                .Where(candle => candle != null)
                .OrderBy(candle => candle.Date)
                .ToList();

            if (candles.Count < 10)
            {
                return null;
            }

            var strongIndex = FindStrongDayIndex(candles);
            if (strongIndex < 4)
            {
                return null;
            }

            var strongDay = candles[strongIndex];
            var latest = candles[candles.Count - 1];
            var previousCandles = candles.Take(strongIndex).ToList();
            var postStrongCandles = candles.Skip(strongIndex + 1).ToList();

            var platformHigh = previousCandles.Skip(Math.Max(0, previousCandles.Count - 6)).Max(candle => candle.High);
            var averageVolumeBeforeStrong = AverageVolume(candles, Math.Max(0, strongIndex - 5), strongIndex - 1);
            var support1 = Math.Round((strongDay.Open + strongDay.Close) / 2m, 2);
            var support2 = Math.Round(strongDay.Low, 2);
            var resistance = Math.Round(new[] { strongDay.High, latest.High, postStrongCandles.Any() ? postStrongCandles.Max(candle => candle.High) : strongDay.High }.Max(), 2);
            var ma5 = Math.Round(candles.Skip(Math.Max(0, candles.Count - 5)).Average(candle => candle.Close), 2);
            var lowestAfterStrong = postStrongCandles.Any() ? postStrongCandles.Min(candle => candle.Low) : latest.Low;
            var averagePostStrongVolume = postStrongCandles.Any() ? postStrongCandles.Average(candle => candle.Volume) : strongDay.Volume;
            var latestChange = strongIndex > 0 && candles[candles.Count - 2].Close > 0
                ? (latest.Close - candles[candles.Count - 2].Close) / candles[candles.Count - 2].Close
                : 0m;

            var signal1 = lowestAfterStrong >= support2;
            var signal2 = latest.Close >= support1 && lowestAfterStrong >= support1 * 0.985m;
            var signal3 = IsBreakoutStrong(strongDay, platformHigh, averageVolumeBeforeStrong, candles, strongIndex);
            var signal4 = !postStrongCandles.Any() || averagePostStrongVolume <= strongDay.Volume * 0.85m || latest.Volume <= strongDay.Volume * 0.9m;

            var isCType = IsCType(candles, strongIndex, support1, support2, strongDay.Volume);
            var isAType = !isCType && IsAType(previousCandles, strongDay, latest, postStrongCandles, platformHigh);

            if (!isAType && !isCType)
            {
                return null;
            }

            if (latest.Close < support2)
            {
                return null;
            }

            var intradayWeak = candidate.Intraday != null &&
                               candidate.Intraday.BreaksAveragePriceAndFails &&
                               !candidate.Intraday.ReclaimsAveragePrice;

            decimal score = 0m;
            score += signal1 ? 24m : 0m;
            score += signal2 ? 22m : 0m;
            score += signal3 ? 24m : 0m;
            score += signal4 ? 18m : 0m;
            score += isCType ? 12m : 8m;
            score += Math.Min(candidate.ConsecutiveLimitUpCount, 3) * 3m;
            score += latestChange > 0 ? 4m : 0m;
            score -= latest.Close < support1 ? 10m : 0m;
            score -= intradayWeak ? 8m : 0m;

            if (score < 55m)
            {
                return null;
            }

            return new StockRecommendation
            {
                Code = candidate.Code,
                Name = candidate.Name,
                PatternType = isCType ? "C类（二买确认）" : "A类（突破加速）",
                Score = Math.Round(score, 1),
                Support1 = support1,
                Support2 = support2,
                Resistance = resistance,
                TomorrowCondition = BuildTomorrowCondition(isCType, support1, support2, resistance),
                StopLoss = support2,
                TakeProfitTrigger = BuildTakeProfitTrigger(ma5, intradayWeak),
                Reason = BuildReason(signal1, signal2, signal3, signal4, candidate.ConsecutiveLimitUpCount, platformHigh),
                WatchItems = BuildWatchItems(support1, support2, ma5, intradayWeak)
            };
        }

        private bool IsEligible(StockCandidateInput candidate)
        {
            if (candidate == null || candidate.IsSt)
            {
                return false;
            }

            var code = (candidate.Code ?? string.Empty).Trim();
            var market = (candidate.Market ?? string.Empty).Trim().ToUpperInvariant();

            if (code.StartsWith("300", StringComparison.Ordinal) ||
                code.StartsWith("688", StringComparison.Ordinal) ||
                code.StartsWith("8", StringComparison.Ordinal) ||
                code.StartsWith("4", StringComparison.Ordinal))
            {
                return false;
            }

            return !market.Contains("BJ") &&
                   !market.Contains("BEIJING") &&
                   !market.Contains("北交");
        }

        private int FindStrongDayIndex(IList<DailyCandle> candles)
        {
            for (var index = candles.Count - 1; index >= 1; index--)
            {
                var current = candles[index];
                var previous = candles[index - 1];
                var averageVolume = AverageVolume(candles, Math.Max(0, index - 5), index - 1);
                var increase = previous.Close > 0 ? (current.Close - previous.Close) / previous.Close : 0m;
                var range = Math.Max(current.High - current.Low, 0.01m);
                var closeNearHigh = (current.Close - current.Low) / range >= 0.72m;

                if (current.IsLimitUp ||
                    (current.Close > current.Open &&
                     increase >= 0.065m &&
                     current.Close > previous.Close &&
                     current.Volume >= averageVolume * 1.5m &&
                     closeNearHigh))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsBreakoutStrong(DailyCandle strongDay, decimal platformHigh, decimal averageVolumeBeforeStrong, IList<DailyCandle> candles, int strongIndex)
        {
            var previousClose = strongIndex > 0 ? candles[strongIndex - 1].Close : strongDay.Open;
            var breakoutRatio = previousClose > 0 ? (strongDay.Close - previousClose) / previousClose : 0m;
            return strongDay.Close >= platformHigh * 1.01m &&
                   (strongDay.IsLimitUp || breakoutRatio >= 0.065m) &&
                   strongDay.Volume >= averageVolumeBeforeStrong * 1.5m;
        }

        private bool IsAType(IList<DailyCandle> previousCandles, DailyCandle strongDay, DailyCandle latest, IList<DailyCandle> postStrongCandles, decimal platformHigh)
        {
            if (previousCandles.Count < 5)
            {
                return false;
            }

            var platformWindow = previousCandles.Skip(Math.Max(0, previousCandles.Count - 6)).ToList();
            var high = platformWindow.Max(candle => candle.High);
            var low = platformWindow.Min(candle => candle.Low);
            var rangeRatio = low > 0 ? (high - low) / low : 1m;
            var holdBreakout = !postStrongCandles.Any() || postStrongCandles.Min(candle => candle.Low) >= platformHigh * 0.985m;

            return rangeRatio <= 0.12m &&
                   strongDay.Close > high &&
                   holdBreakout &&
                   latest.Close >= platformHigh;
        }

        private bool IsCType(IList<DailyCandle> candles, int strongIndex, decimal support1, decimal support2, long strongVolume)
        {
            var daysAfterStrong = candles.Count - 1 - strongIndex;
            if (daysAfterStrong < 1 || daysAfterStrong > 3)
            {
                return false;
            }

            var postStrongCandles = candles.Skip(strongIndex + 1).ToList();
            if (!postStrongCandles.Any())
            {
                return false;
            }

            var latest = postStrongCandles.Last();
            var hadPullback = postStrongCandles.Take(postStrongCandles.Count - 1)
                .Any(candle => candle.Close <= candle.Open || candle.Low <= support1 * 1.01m);

            var postLow = postStrongCandles.Min(candle => candle.Low);
            var averageVolume = postStrongCandles.Average(candle => candle.Volume);
            var latestTurnsStrong = latest.Close >= support1 &&
                                    latest.Close >= postStrongCandles.Max(candle => candle.Open) &&
                                    latest.Close >= candles[strongIndex].Open;

            return hadPullback &&
                   postLow >= support2 &&
                   averageVolume <= strongVolume * 0.9m &&
                   latestTurnsStrong;
        }

        private decimal AverageVolume(IList<DailyCandle> candles, int startIndex, int endIndex)
        {
            if (startIndex > endIndex || !candles.Any())
            {
                return candles.Any() ? candles.Last().Volume : 0m;
            }

            return candles
                .Skip(startIndex)
                .Take(endIndex - startIndex + 1)
                .Average(candle => candle.Volume);
        }

        private string BuildTomorrowCondition(bool isCType, decimal support1, decimal support2, decimal resistance)
        {
            if (isCType)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "强：回踩 {0:F2} 不破并放量越过 {1:F2}；弱：跌破 {2:F2} 后站不回。",
                    support1,
                    resistance,
                    support1);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "强：高开或震荡后守住 {0:F2}，继续放量冲击 {1:F2}；弱：跌回突破区并逼近 {2:F2}。",
                support1,
                resistance,
                support2);
        }

        private string BuildTakeProfitTrigger(decimal ma5, bool intradayWeak)
        {
            return intradayWeak
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "优先看加速失败：冲高不封板且放量长上影/回封无力先减仓；若跌破 MA5({0:F2}) 且分时均价线反抽不过，继续止盈。",
                    ma5)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "优先看加速失败：冲高不封板且放量长上影/回封无力分批止盈；若跌破 MA5({0:F2}) 或分时均价线反抽不过，继续减仓。",
                    ma5);
        }

        private string BuildReason(bool signal1, bool signal2, bool signal3, bool signal4, int consecutiveLimitUpCount, decimal platformHigh)
        {
            var reasons = new List<string>();

            if (signal1)
            {
                reasons.Add("回踩未创新低");
            }

            if (signal2)
            {
                reasons.Add("震荡换手后仍守关键位");
            }

            if (signal3)
            {
                reasons.Add(string.Format(CultureInfo.InvariantCulture, "放量越过平台高点 {0:F2}", platformHigh));
            }

            if (signal4)
            {
                reasons.Add("回踩缩量承接尚可");
            }

            if (consecutiveLimitUpCount > 0)
            {
                reasons.Add(string.Format(CultureInfo.InvariantCulture, "当前连板数 {0}", consecutiveLimitUpCount));
            }

            return string.Join("；", reasons);
        }

        private List<string> BuildWatchItems(decimal support1, decimal support2, decimal ma5, bool intradayWeak)
        {
            var items = new List<string>
            {
                string.Format(CultureInfo.InvariantCulture, "重要支撑1：最近强K实体中位 {0:F2}，破位且站不回视为转弱。", support1),
                string.Format(CultureInfo.InvariantCulture, "重要支撑2：最近强K最低价 {0:F2}，有效跌破执行止损。", support2),
                string.Format(CultureInfo.InvariantCulture, "跟踪 MA5：{0:F2}，失守后优先防加速失败。", ma5),
                "观察分时：跌破均价线后反抽不过，是走弱确认信号。"
            };

            if (intradayWeak)
            {
                items.Insert(0, "当日分时已出现均价线跌破后反抽受阻，次日只适合弱转强确认，不能直接追高。");
            }

            return items;
        }
    }
}
