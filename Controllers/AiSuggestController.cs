using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
using SalonHair.Models.SalonHair.Models;
using SalonHair.Services;
using System.Globalization;
using System.Text;

namespace SalonHair.Controllers
{
    public class AiSuggestController : Controller
    {
        private readonly SalonContext _context;
        private readonly AiFeedbackLocalStore _feedbackStore;
        private readonly AiFeedbackModelTrainer _modelTrainer;

        public AiSuggestController(
            SalonContext context,
            AiFeedbackLocalStore feedbackStore,
            AiFeedbackModelTrainer modelTrainer)
        {
            _context = context;
            _feedbackStore = feedbackStore;
            _modelTrainer = modelTrainer;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze([FromBody] FaceScanAnalysisRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DetectedShape))
            {
                return BadRequest(new { message = "Chưa nhận được dữ liệu khuôn mặt để phân tích." });
            }

            var normalizedShape = NormalizeShape(request.DetectedShape);
            if (string.IsNullOrWhiteSpace(normalizedShape))
            {
                return BadRequest(new { message = "Không xác định được dáng khuôn mặt." });
            }

            var model = await BuildSuggestionViewModelAsync(normalizedShape, request.Confidence, manualMode: false);
            model.Summary = BuildSummary(normalizedShape, request, manualMode: false);

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFeedback([FromBody] AiFeedbackSaveRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Không có dữ liệu feedback để lưu." });
            }

            var predictedShape = NormalizeShape(request.PredictedShape);
            var correctedShape = NormalizeShape(request.CorrectedShape ?? request.PredictedShape);

            if (string.IsNullOrWhiteSpace(predictedShape) || string.IsNullOrWhiteSpace(correctedShape))
            {
                return BadRequest(new { message = "Feedback chưa có nhãn dáng mặt hợp lệ." });
            }

            var sample = await _feedbackStore.SaveAsync(request, predictedShape, correctedShape, cancellationToken);
            var stats = await _feedbackStore.GetStatsAsync(cancellationToken);

            return Json(new
            {
                message = sample.IsPredictionAccepted
                    ? "Đã lưu mẫu đúng để bộ dữ liệu học của salon dày hơn."
                    : $"Đã lưu mẫu sửa lại thành {sample.CorrectedShape}.",
                sampleId = sample.SampleId,
                correctedShape = sample.CorrectedShape,
                totalSamples = stats.TotalSamples,
                storageMode = "local-export-import"
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportFeedback(CancellationToken cancellationToken)
        {
            var export = await _feedbackStore.ExportAsync(cancellationToken);
            return File(export.Content, "application/zip", export.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> ImportFeedback(IFormFile? datasetArchive, CancellationToken cancellationToken)
        {
            if (datasetArchive == null || datasetArchive.Length == 0)
            {
                return BadRequest(new { message = "Bạn cần chọn file zip dataset để import." });
            }

            await using var stream = datasetArchive.OpenReadStream();
            var result = await _feedbackStore.ImportAsync(stream, cancellationToken);

            return Json(new
            {
                message = $"Import xong {result.ImportedSamples} mẫu mới, bỏ qua {result.SkippedSamples} mẫu trùng.",
                importedSamples = result.ImportedSamples,
                skippedSamples = result.SkippedSamples,
                importedSnapshots = result.ImportedSnapshots,
                totalSamples = result.TotalSamples
            });
        }

        [HttpPost]
        public async Task<IActionResult> TrainModel(CancellationToken cancellationToken)
        {
            var result = await _modelTrainer.TrainAsync(cancellationToken);

            return Json(new
            {
                modelCreated = result.ModelCreated,
                totalSamples = result.TotalSamples,
                distinctShapes = result.DistinctShapes,
                modelPath = result.ModelPath,
                modelVersion = result.ModelVersion,
                message = result.Message
            });
        }

        [HttpGet]
        public async Task<IActionResult> RuntimeModel(CancellationToken cancellationToken)
        {
            var model = await _feedbackStore.LoadModelAsync(cancellationToken);
            if (model == null)
            {
                return NotFound(new { message = "Chưa có model đã train trên máy này." });
            }

            return Json(model);
        }

        public async Task<IActionResult> Result(string shape)
        {
            var normalizedShape = NormalizeShape(shape);
            if (string.IsNullOrWhiteSpace(normalizedShape))
            {
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildSuggestionViewModelAsync(normalizedShape, 0.68, manualMode: true);
            return View(model);
        }

        private async Task<AiSuggestionViewModel> BuildSuggestionViewModelAsync(string shape, double confidence, bool manualMode)
        {
            var suggestions = await GetSuggestionsAsync(shape);

            return new AiSuggestionViewModel
            {
                FaceShape = shape,
                Confidence = Math.Clamp(confidence, 0.55, 0.96),
                ConfidenceLabel = manualMode
                    ? "Tư vấn theo lựa chọn thủ công của khách."
                    : "AI đã quét ảnh chân dung và ước lượng tỉ lệ khuôn mặt.",
                Summary = BuildSummary(shape, request: null, manualMode),
                StylingTips = GetStylingTips(shape),
                Suggestions = suggestions.Items,
                UsedFallbackData = suggestions.UsedFallbackData
            };
        }

        private async Task<(List<Hairstyle> Items, bool UsedFallbackData)> GetSuggestionsAsync(string shape)
        {
            try
            {
                var suggestions = (await _context.Hairstyles
                        .Where(h => h.FaceShape != null)
                        .ToListAsync())
                    .Where(h => NormalizeShape(h.FaceShape) == shape)
                    .ToList();

                if (suggestions.Any())
                {
                    ApplyCuratedImages(suggestions);
                    return (suggestions, false);
                }
            }
            catch
            {
                // Database is optional for this feature. Fallback data is returned below.
            }

            return (GetFallbackSuggestions(shape), true);
        }

        private static void ApplyCuratedImages(List<Hairstyle> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                var curatedUrl = GetCuratedImageUrl(suggestion.StyleName);
                if (!string.IsNullOrWhiteSpace(curatedUrl))
                {
                    suggestion.ImageUrl = curatedUrl;
                }
            }
        }

        private static string? GetCuratedImageUrl(string? styleName)
        {
            var key = NormalizeStyleKey(styleName);

            return key switch
            {
                "high-fade-pompadour" => "https://cdn.shopify.com/s/files/1/0029/0868/4397/files/Fade-Pompadour.webp?v=1754905431",
                "layer-layer" => "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=900&q=80",
                "side-part-7-3" => "https://cellphones.com.vn/sforum/wp-content/uploads/2024/04/toc-side-part-7-3-30.jpeg",
                "crew-cut" => "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?auto=format&fit=crop&w=900&q=80",
                "undercut-vuot-nguoc" => "https://images.unsplash.com/photo-1521119989659-a83eee488004?auto=format&fit=crop&w=900&q=80",
                "ivy-league" => "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&w=900&q=80",
                "buzz-cut" => "https://haircutinspiration.com/wp-content/uploads/Pitch-Perfect-Buzz-Cut.jpg",
                "mullet-thoi-thuong" => "https://cdn11.dienmaycholon.vn/filewebdmclnew/public/userupload/files/Image%20FP_2024/layer-mullet-1.jpg",
                "uon-xoan-nhe" => "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80",
                "middle-part-bo-luong" => "https://xwatch.vn/upload_images/images/2023/03/10/toc-middle-part-5-5.gif",
                "side-swept" => "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80",
                "toc-mai-fringe" => "https://liembarbershop.com/wp-content/uploads/2024/08/Long-Fringe-03.jpg",
                _ => null
            };
        }

        private static string NormalizeStyleKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            var previousWasDash = false;

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                    previousWasDash = false;
                }
                else if (!previousWasDash)
                {
                    builder.Append('-');
                    previousWasDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        private static string NormalizeShape(string? shape)
        {
            if (string.IsNullOrWhiteSpace(shape))
            {
                return string.Empty;
            }

            var value = shape.Trim().ToLowerInvariant();

            if (value.Contains("tròn") || value.Contains("trã²n") || value.Contains("tron") || value.Contains("round"))
            {
                return "Tròn";
            }

            if (value.Contains("vuông") || value.Contains("vuã´ng") || value.Contains("vuong") || value.Contains("square"))
            {
                return "Vuông";
            }

            if (value.Contains("xoan") || value.Contains("oval"))
            {
                return "Trái xoan";
            }

            if (value.Contains("dài") || value.Contains("dã i") || value.Contains("dai") || value.Contains("long") || value.Contains("oblong"))
            {
                return "Dài";
            }

            return string.Empty;
        }

        private static string BuildSummary(string shape, FaceScanAnalysisRequest? request, bool manualMode)
        {
            if (manualMode)
            {
                return shape switch
                {
                    "Tròn" => "Khuôn mặt tròn hợp kiểu tóc có độ phồng ở đỉnh đầu và ôm gọn hai bên để tổng thể thanh hơn.",
                    "Vuông" => "Khuôn mặt vuông hợp kiểu tóc có texture và độ mềm vừa phải để làm dịu góc hàm nhưng vẫn nam tính.",
                    "Dài" => "Khuôn mặt dài nên ưu tiên kiểu có mái hoặc rẽ ngôi để cân lại chiều dọc và tạo cảm giác đầy hơn.",
                    _ => "Khuôn mặt trái xoan có tỉ lệ cân đối, dễ hợp nhiều kiểu từ lịch sự đến cá tính."
                };
            }

            if (request == null)
            {
                return "AI đã hoàn tất việc quét khuôn mặt và chọn nhóm kiểu tóc phù hợp nhất.";
            }

            return shape switch
            {
                "Tròn" => $"AI nhận thấy tỉ lệ dài/rộng khoảng {request.FaceLengthRatio:F2}, phù hợp nhóm mặt tròn. Nên ưu tiên kiểu tăng chiều cao phần đỉnh để mặt trông gọn hơn.",
                "Vuông" => $"AI thấy phần trán và hàm khá cân bằng, chênh lệch khoảng {request.ForeheadJawDelta:P0}. Những kiểu có layer hoặc side-part sẽ giúp gương mặt mềm hơn.",
                "Dài" => $"AI nhận thấy gương mặt thiên dài với tỉ lệ dài/rộng khoảng {request.FaceLengthRatio:F2}. Các kiểu có mái, rẽ ngôi hoặc độ phủ ngang sẽ cân mặt tốt hơn.",
                _ => "AI nhận thấy gương mặt khá cân đối, phù hợp nhóm trái xoan. Bạn có thể thử nhiều kiểu tóc linh hoạt hơn."
            };
        }

        private static List<string> GetStylingTips(string shape)
        {
            return shape switch
            {
                "Tròn" => new List<string>
                {
                    "Ưu tiên fade gọn hai bên và để volume trên đỉnh đầu.",
                    "Tránh mái bằng dày che trọn toàn bộ trán.",
                    "Hợp với quiff, pompadour, side part và textured crop."
                },
                "Vuông" => new List<string>
                {
                    "Nên có texture hoặc uốn nhẹ để làm dịu đường nét.",
                    "Giữ hai bên gọn nhưng không nên cắt sát quá mức.",
                    "Hợp với crew cut, ivy league, side sweep và slick back mềm."
                },
                "Dài" => new List<string>
                {
                    "Không nên đẩy phần đỉnh quá cao vì sẽ làm mặt dài hơn.",
                    "Nên có mái, rẽ ngôi hoặc độ phủ ngang để cân bằng tổng thể.",
                    "Hợp với middle part, fringe, side swept và layer mềm."
                },
                _ => new List<string>
                {
                    "Khuôn mặt cân đối nên có thể thử từ classic đến modern.",
                    "Có thể ưu tiên texture, mullet mềm, crop hoặc uốn nhẹ.",
                    "Hãy chốt kiểu theo phong cách cá nhân và chất tóc thực tế."
                }
            };
        }

        private static List<Hairstyle> GetFallbackSuggestions(string shape)
        {
            return shape switch
            {
                "Tròn" => new List<Hairstyle>
                {
                    new() { FaceShape = "Tròn", StyleName = "High Fade Pompadour", Description = "Tăng chiều cao phần đỉnh đầu, giúp mặt trông dài và gọn hơn.", ImageUrl = "https://images.unsplash.com/photo-1517832606299-7ae9b720a186?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Tròn", StyleName = "Side Part 7/3", Description = "Tạo đường chia tóc rõ ràng để gương mặt thanh thoát và lịch sự hơn.", ImageUrl = "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Tròn", StyleName = "Textured Quiff", Description = "Giữ texture tự nhiên nhưng vẫn kéo dọc gương mặt rất tốt.", ImageUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=900&q=80" }
                },
                "Vuông" => new List<Hairstyle>
                {
                    new() { FaceShape = "Vuông", StyleName = "Ivy League", Description = "Gọn gàng, nam tính và giữ được độ sang cho khuôn mặt góc cạnh.", ImageUrl = "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Vuông", StyleName = "Soft Slick Back", Description = "Vuốt ngược nhẹ với độ mềm vừa đủ để cân phần xương hàm.", ImageUrl = "https://images.unsplash.com/photo-1521119989659-a83eee488004?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Vuông", StyleName = "Layer Side Sweep", Description = "Giữ mái đổ chéo giúp đường nét khuôn mặt hài hòa hơn.", ImageUrl = "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&w=900&q=80" }
                },
                "Dài" => new List<Hairstyle>
                {
                    new() { FaceShape = "Dài", StyleName = "Middle Part", Description = "Chia ngôi giữa giúp cân lại chiều dài khuôn mặt và tăng độ thư sinh.", ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Dài", StyleName = "Fringe Layer", Description = "Phần mái giúp gương mặt ngắn và cân đối hơn khi nhìn trực diện.", ImageUrl = "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Dài", StyleName = "Side Swept", Description = "Vuốt lệch tự nhiên để thêm độ ngang và giảm cảm giác mặt dài.", ImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80" }
                },
                _ => new List<Hairstyle>
                {
                    new() { FaceShape = "Trái xoan", StyleName = "Textured Crop", Description = "Dễ phối với nhiều phong cách từ trẻ trung đến lịch lãm.", ImageUrl = "https://images.unsplash.com/photo-1504257432389-52343af06ae3?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Trái xoan", StyleName = "Modern Mullet", Description = "Một lựa chọn cá tính nhưng vẫn cân đối với khuôn mặt trái xoan.", ImageUrl = "https://images.unsplash.com/photo-1519996529931-28324d5a630e?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Trái xoan", StyleName = "Loose Wave", Description = "Uốn nhẹ tạo chiều sâu và cảm giác thời trang hơn.", ImageUrl = "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80" }
                }
            };
        }
    }
}
