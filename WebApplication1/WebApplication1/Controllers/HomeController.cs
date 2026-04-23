using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly StockWaveAnalysisService _analysisService = new StockWaveAnalysisService();

        [HttpGet]
        public ActionResult Index()
        {
            return View(new IndexPageViewModel
            {
                RawJson = SampleInputJson()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(IndexPageViewModel model)
        {
            model = model ?? new IndexPageViewModel();
            model.RawJson = string.IsNullOrWhiteSpace(model.RawJson) ? SampleInputJson() : model.RawJson;
            model.Recommendations = new List<StockRecommendation>();

            try
            {
                var candidates = JsonConvert.DeserializeObject<List<StockCandidateInput>>(model.RawJson);
                model.Recommendations = new List<StockRecommendation>(_analysisService.Analyze(candidates));

                if (!model.Recommendations.Any())
                {
                    model.ErrorMessage = "没有筛出满足主力浪潮结构的标的；请检查是否提供了主板非 ST 个股，以及足够的K线与量能数据。";
                }
            }
            catch (JsonException)
            {
                model.ErrorMessage = "输入 JSON 解析失败，请按示例结构提供候选股票数组。";
            }

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private string SampleInputJson()
        {
            return @"[
  {
    ""code"": ""600123"",
    ""name"": ""浪潮样本A"",
    ""market"": ""SH"",
    ""isSt"": false,
    ""consecutiveLimitUpCount"": 1,
    ""intraday"": {
      ""breaksAveragePriceAndFails"": false,
      ""reclaimsAveragePrice"": true
    },
    ""candles"": [
      { ""date"": ""2026-04-07"", ""open"": 10.10, ""high"": 10.18, ""low"": 10.00, ""close"": 10.12, ""volume"": 120000 },
      { ""date"": ""2026-04-08"", ""open"": 10.12, ""high"": 10.22, ""low"": 10.05, ""close"": 10.16, ""volume"": 118000 },
      { ""date"": ""2026-04-09"", ""open"": 10.15, ""high"": 10.20, ""low"": 10.06, ""close"": 10.13, ""volume"": 110000 },
      { ""date"": ""2026-04-10"", ""open"": 10.14, ""high"": 10.21, ""low"": 10.08, ""close"": 10.18, ""volume"": 125000 },
      { ""date"": ""2026-04-11"", ""open"": 10.18, ""high"": 10.25, ""low"": 10.10, ""close"": 10.20, ""volume"": 121000 },
      { ""date"": ""2026-04-14"", ""open"": 10.20, ""high"": 10.28, ""low"": 10.15, ""close"": 10.22, ""volume"": 123000 },
      { ""date"": ""2026-04-15"", ""open"": 10.25, ""high"": 11.05, ""low"": 10.23, ""close"": 11.02, ""volume"": 260000, ""isLimitUp"": true },
      { ""date"": ""2026-04-16"", ""open"": 10.98, ""high"": 11.08, ""low"": 10.84, ""close"": 10.92, ""volume"": 180000 },
      { ""date"": ""2026-04-17"", ""open"": 10.90, ""high"": 11.00, ""low"": 10.86, ""close"": 10.95, ""volume"": 150000 },
      { ""date"": ""2026-04-18"", ""open"": 10.96, ""high"": 11.18, ""low"": 10.93, ""close"": 11.15, ""volume"": 175000 }
    ]
  },
  {
    ""code"": ""000789"",
    ""name"": ""浪潮样本C"",
    ""market"": ""SZ"",
    ""isSt"": false,
    ""consecutiveLimitUpCount"": 2,
    ""intraday"": {
      ""breaksAveragePriceAndFails"": false,
      ""reclaimsAveragePrice"": true
    },
    ""candles"": [
      { ""date"": ""2026-04-07"", ""open"": 8.82, ""high"": 8.90, ""low"": 8.76, ""close"": 8.84, ""volume"": 135000 },
      { ""date"": ""2026-04-08"", ""open"": 8.84, ""high"": 8.91, ""low"": 8.80, ""close"": 8.86, ""volume"": 131000 },
      { ""date"": ""2026-04-09"", ""open"": 8.87, ""high"": 8.95, ""low"": 8.83, ""close"": 8.90, ""volume"": 129000 },
      { ""date"": ""2026-04-10"", ""open"": 8.90, ""high"": 8.96, ""low"": 8.85, ""close"": 8.88, ""volume"": 127000 },
      { ""date"": ""2026-04-11"", ""open"": 8.87, ""high"": 8.94, ""low"": 8.82, ""close"": 8.89, ""volume"": 133000 },
      { ""date"": ""2026-04-14"", ""open"": 8.89, ""high"": 8.97, ""low"": 8.84, ""close"": 8.93, ""volume"": 136000 },
      { ""date"": ""2026-04-15"", ""open"": 8.95, ""high"": 9.82, ""low"": 8.92, ""close"": 9.82, ""volume"": 310000, ""isLimitUp"": true },
      { ""date"": ""2026-04-16"", ""open"": 9.78, ""high"": 9.81, ""low"": 9.46, ""close"": 9.55, ""volume"": 205000 },
      { ""date"": ""2026-04-17"", ""open"": 9.54, ""high"": 9.63, ""low"": 9.48, ""close"": 9.58, ""volume"": 160000 },
      { ""date"": ""2026-04-18"", ""open"": 9.60, ""high"": 9.92, ""low"": 9.57, ""close"": 9.90, ""volume"": 190000 }
    ]
  },
  {
    ""code"": ""300456"",
    ""name"": ""创业板过滤样本"",
    ""market"": ""SZ"",
    ""isSt"": false,
    ""consecutiveLimitUpCount"": 1,
    ""candles"": [
      { ""date"": ""2026-04-07"", ""open"": 15.00, ""high"": 15.30, ""low"": 14.90, ""close"": 15.10, ""volume"": 100000 },
      { ""date"": ""2026-04-08"", ""open"": 15.12, ""high"": 15.26, ""low"": 14.98, ""close"": 15.20, ""volume"": 102000 },
      { ""date"": ""2026-04-09"", ""open"": 15.25, ""high"": 15.36, ""low"": 15.10, ""close"": 15.28, ""volume"": 99000 },
      { ""date"": ""2026-04-10"", ""open"": 15.28, ""high"": 16.10, ""low"": 15.26, ""close"": 16.08, ""volume"": 240000, ""isLimitUp"": true },
      { ""date"": ""2026-04-11"", ""open"": 16.04, ""high"": 16.18, ""low"": 15.95, ""close"": 16.12, ""volume"": 180000 },
      { ""date"": ""2026-04-14"", ""open"": 16.10, ""high"": 16.26, ""low"": 16.00, ""close"": 16.20, ""volume"": 150000 },
      { ""date"": ""2026-04-15"", ""open"": 16.18, ""high"": 16.38, ""low"": 16.10, ""close"": 16.35, ""volume"": 148000 },
      { ""date"": ""2026-04-16"", ""open"": 16.34, ""high"": 16.45, ""low"": 16.20, ""close"": 16.28, ""volume"": 140000 },
      { ""date"": ""2026-04-17"", ""open"": 16.28, ""high"": 16.52, ""low"": 16.24, ""close"": 16.50, ""volume"": 170000 },
      { ""date"": ""2026-04-18"", ""open"": 16.52, ""high"": 16.70, ""low"": 16.44, ""close"": 16.60, ""volume"": 175000 }
    ]
  }
]";
        }
    }
}
