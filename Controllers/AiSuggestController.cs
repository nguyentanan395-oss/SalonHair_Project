using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
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
            var normalizedGender = NormalizeGender(request.Gender);
            var normalizedAgeGroup = NormalizeAgeGroup(request.AgeGroup);

            if (string.IsNullOrWhiteSpace(normalizedShape))
            {
                return BadRequest(new { message = "Không xác định được dáng khuôn mặt." });
            }

            if (string.IsNullOrWhiteSpace(normalizedGender) || string.IsNullOrWhiteSpace(normalizedAgeGroup))
            {
                return BadRequest(new { message = "Vui lòng chọn giới tính và độ tuổi trước khi phân tích khuôn mặt." });
            }

            var model = await BuildSuggestionViewModelAsync(normalizedShape, request.Confidence, manualMode: false, normalizedGender, normalizedAgeGroup);
            model.Summary = BuildSummary(normalizedShape, request, manualMode: false, normalizedGender, normalizedAgeGroup);

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

        public async Task<IActionResult> Result(string shape, string? gender = null, string? ageGroup = null)
        {
            var normalizedShape = NormalizeShape(shape);
            var normalizedGender = NormalizeGender(gender);
            var normalizedAgeGroup = NormalizeAgeGroup(ageGroup);

            if (string.IsNullOrWhiteSpace(normalizedShape) || string.IsNullOrWhiteSpace(normalizedGender) || string.IsNullOrWhiteSpace(normalizedAgeGroup))
            {
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildSuggestionViewModelAsync(normalizedShape, 0.68, manualMode: true, normalizedGender, normalizedAgeGroup);
            return View(model);
        }

        private async Task<AiSuggestionViewModel> BuildSuggestionViewModelAsync(
            string shape,
            double confidence,
            bool manualMode,
            string? gender = null,
            string? ageGroup = null)
        {
            var suggestions = await GetSuggestionsAsync(shape, gender, ageGroup);
            var tailoredSuggestions = TailorSuggestions(suggestions.Items, gender, ageGroup);
            NormalizeSuggestionImageUrls(tailoredSuggestions);

            return new AiSuggestionViewModel
            {
                FaceShape = shape,
                Gender = gender,
                AgeGroup = ageGroup,
                Confidence = Math.Clamp(confidence, 0.55, 0.96),
                ConfidenceLabel = manualMode
                    ? "Tư vấn theo lựa chọn thủ công của khách."
                    : "AI đã quét ảnh chân dung và ước lượng tỉ lệ khuôn mặt.",
                Summary = BuildSummary(shape, request: null, manualMode, gender, ageGroup),
                StylingTips = GetStylingTips(shape, gender, ageGroup),
                Suggestions = tailoredSuggestions,
                UsedFallbackData = suggestions.UsedFallbackData
            };
        }

        private async Task<(List<Hairstyle> Items, bool UsedFallbackData)> GetSuggestionsAsync(string shape, string? gender, string? ageGroup)
        {
            try
            {
                var suggestions = (await _context.Hairstyles
        .Where(h => h.FaceShape != null && h.Gender != null)
        .ToListAsync())
    .Where(h =>
        NormalizeShape(h.FaceShape) == shape &&
        NormalizeGender(h.Gender) == gender &&
        NormalizeAgeGroup(h.AgeGroup) == ageGroup)
    .ToList();

                if (suggestions.Any())
                {
                    var profileFilteredSuggestions = ApplyProfileFilters(suggestions, gender, ageGroup);
                    if (profileFilteredSuggestions.Any())
                    {
                        ApplyCuratedImages(profileFilteredSuggestions);
                        return (profileFilteredSuggestions, false);
                    }
                }
            }
            catch
            {
                // Database is optional for this feature. Fallback data is returned below.
            }

            var fallbackSuggestions = GetFallbackSuggestions(shape, gender);
            var fallbackFilteredSuggestions = ApplyProfileFilters(fallbackSuggestions, gender, ageGroup);
            ApplyCuratedImages(fallbackFilteredSuggestions);
            return (fallbackFilteredSuggestions, true);
        }

        private static List<Hairstyle> TailorSuggestions(List<Hairstyle> suggestions, string? gender, string? ageGroup)
        {
            var tailored = suggestions
                .Select(suggestion =>
                {
                    suggestion.Description = AppendProfileNotes(suggestion.Description, gender, ageGroup);
                    return suggestion;
                })
                .ToList();

            if (string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase))
            {
                tailored = tailored
                    .OrderByDescending(item => item.StyleName.Contains("Fade", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Cut", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Part", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Slick", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(item => item.StyleName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else if (string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase))
            {
                tailored = tailored
                    .OrderByDescending(item => item.StyleName.Contains("Wave", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Layer", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Fringe", StringComparison.OrdinalIgnoreCase)
                        || item.StyleName.Contains("Part", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(item => item.StyleName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (string.Equals(ageGroup, "46+", StringComparison.OrdinalIgnoreCase))
            {
                tailored = tailored
                    .OrderBy(item => item.StyleName.Contains("Fade", StringComparison.OrdinalIgnoreCase) || item.StyleName.Contains("Cut", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(item => item.StyleName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return tailored;
        }

        private static string AppendProfileNotes(string? description, string? gender, string? ageGroup)
        {
            var notes = new List<string>();

            if (string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("đề xuất theo phong cách nam tính, dễ chăm sóc và giữ form tốt.");
            }
            else if (string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("đề xuất theo góc nhìn mềm mại, tinh tế và cân đối với đường nét nữ.");
            }

            if (string.Equals(ageGroup, "Dưới 18", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("ưu tiên kiểu trẻ trung, nhiều texture và dễ phối xu hướng.");
            }
            else if (string.Equals(ageGroup, "18-30", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("ưu tiên kiểu hiện đại, năng động và dễ thử nhiều phong cách.");
            }
            else if (string.Equals(ageGroup, "31-45", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("ưu tiên kiểu cân bằng giữa hiện đại, gọn gàng và dễ duy trì.");
            }
            else if (string.Equals(ageGroup, "46+", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("ưu tiên kiểu đơn giản, mềm mại và dễ chỉnh trong ngày thường.");
            }

            if (notes.Count == 0)
            {
                return description ?? string.Empty;
            }

            var baseDescription = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
            var appended = string.Join(" ", notes);

            return string.IsNullOrWhiteSpace(baseDescription)
                ? appended
                : $"{baseDescription} {appended}";
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

        private void NormalizeSuggestionImageUrls(List<Hairstyle> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                if (string.IsNullOrWhiteSpace(suggestion.ImageUrl))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(suggestion.ImageUrl)) continue;

                if (suggestion.ImageUrl.StartsWith("~") || suggestion.ImageUrl.StartsWith("/"))
                // Kiểm tra xem có phải là URL tuyệt đối (http/https) không
                bool isAbsolute = Uri.TryCreate(suggestion.ImageUrl, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!isAbsolute)
                {
                    suggestion.ImageUrl = Url.Content(suggestion.ImageUrl);
                    var path = suggestion.ImageUrl.Trim();
                    // Đảm bảo đường dẫn local luôn bắt đầu bằng ~/ để Url.Content xử lý chính xác
                    if (!path.StartsWith("~") && !path.StartsWith("/"))
                    {
                        path = "~/" + path;
                    }
                    else if (path.StartsWith("/"))
                    {
                        path = "~" + path;
                    }
                    suggestion.ImageUrl = Url.Content(path);
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
                "side-part-7-3" => "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=900&q=80",
                "crew-cut" => "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?auto=format&fit=crop&w=900&q=80",
                "undercut-vuot-nguoc" => "https://images.unsplash.com/photo-1521119989659-a83eee488004?auto=format&fit=crop&w=900&q=80",
                "ivy-league" => "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&w=900&q=80",
                "buzz-cut" => "https://haircutinspiration.com/wp-content/uploads/Pitch-Perfect-Buzz-Cut.jpg",
                "mullet-thoi-thuong" => "https://cdn11.dienmaycholon.vn/filewebdmclnew/public/userupload/files/Image%20FP_2024/layer-mullet-1.jpg",
                "mullet-thoi-thuong" => "https://images.unsplash.com/photo-1519996529931-28324d5a630e?auto=format&fit=crop&w=900&q=80",
                "uon-xoan-nhe" => "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80",
                "middle-part-bo-luong" => "https://xwatch.vn/upload_images/images/2023/03/10/toc-middle-part-5-5.gif",
                "side-swept" => "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80",
                "toc-mai-fringe" => "https://liembarbershop.com/wp-content/uploads/2024/08/Long-Fringe-03.jpg",
                "middle-part-bo-luong" => "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80",
                "side-swept" => "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&w=900&q=80",
                "toc-mai-fringe" => "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80",
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

        private static string NormalizeGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant();

            if (normalized == "nam" || normalized.Contains("nam"))
            {
                return "Nam";
            }

            if (normalized == "nu" || normalized == "nữ" || normalized.Contains("nu") || normalized.Contains("nữ"))
            {
                return "Nữ";
            }

            return string.Empty;
        }

        private static string NormalizeAgeGroup(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant();

            if (normalized == "duoi-18" || normalized == "dưới 18" || normalized == "under 18" || normalized == "under18")
            {
                return "Dưới 18";
            }

            if (normalized == "18-30" || normalized == "18 30" || normalized == "18-30 tuổi" || normalized == "18-30" || normalized == "18-30")
            {
                return "18-30";
            }

            if (normalized == "31-45" || normalized == "31 45" || normalized == "31-45 tuổi")
            {
                return "31-45";
            }

            if (normalized == "46+" || normalized == "46 plus" || normalized == "46 trở lên" || normalized == "46up")
            {
                return "46+";
            }

            return string.Empty;
        }

        private static string BuildProfileHint(string? gender, string? ageGroup)
        {
            var pieces = new List<string>();

            if (!string.IsNullOrWhiteSpace(gender))
            {
                pieces.Add($"giới tính {gender.ToLowerInvariant()}");
            }

            if (!string.IsNullOrWhiteSpace(ageGroup))
            {
                pieces.Add($"độ tuổi {ageGroup}");
            }

            return pieces.Count == 0
                ? string.Empty
                : $" Gợi ý được điều chỉnh theo {string.Join(" và ", pieces)}.";
        }

        private static string BuildSummary(string shape, FaceScanAnalysisRequest? request, bool manualMode, string? gender, string? ageGroup)
        {
            var profileHint = BuildProfileHint(gender, ageGroup);

            if (manualMode)
            {
                var baseSummary = shape switch
                {
                    "Tròn" => "Khuôn mặt tròn hợp kiểu tóc có độ phồng ở đỉnh đầu và ôm gọn hai bên để tổng thể thanh hơn.",
                    "Vuông" => "Khuôn mặt vuông hợp kiểu tóc có texture và độ mềm vừa phải để làm dịu góc hàm nhưng vẫn nam tính.",
                    "Dài" => "Khuôn mặt dài nên ưu tiên kiểu có mái hoặc rẽ ngôi để cân lại chiều dọc và tạo cảm giác đầy hơn.",
                    _ => "Khuôn mặt trái xoan có tỉ lệ cân đối, dễ hợp nhiều kiểu từ lịch sự đến cá tính."
                };

                return string.IsNullOrWhiteSpace(profileHint) ? baseSummary : $"{baseSummary}{profileHint}";
            }

            if (request == null)
            {
                return string.IsNullOrWhiteSpace(profileHint)
                    ? "AI đã hoàn tất việc quét khuôn mặt và chọn nhóm kiểu tóc phù hợp nhất."
                    : $"AI đã hoàn tất việc quét khuôn mặt và chọn nhóm kiểu tóc phù hợp nhất.{profileHint}";
            }

            var measurementSummary = shape switch
            {
                "Tròn" => $"AI nhận thấy tỉ lệ dài/rộng khoảng {request.FaceLengthRatio:F2}, phù hợp nhóm mặt tròn. Nên ưu tiên kiểu tăng chiều cao phần đỉnh để mặt trông gọn hơn.",
                "Vuông" => $"AI thấy phần trán và hàm khá cân bằng, chênh lệch khoảng {request.ForeheadJawDelta:P0}. Những kiểu có layer hoặc side-part sẽ giúp gương mặt mềm hơn.",
                "Dài" => $"AI nhận thấy gương mặt thiên dài với tỉ lệ dài/rộng khoảng {request.FaceLengthRatio:F2}. Các kiểu có mái, rẽ ngôi hoặc độ phủ ngang sẽ cân mặt tốt hơn.",
                _ => "AI nhận thấy gương mặt khá cân đối, phù hợp nhóm trái xoan. Bạn có thể thử nhiều kiểu tóc linh hoạt hơn."
            };

            return string.IsNullOrWhiteSpace(profileHint) ? measurementSummary : $"{measurementSummary}{profileHint}";
        }

        private static List<string> GetStylingTips(string shape, string? gender, string? ageGroup)
        {
            var tips = shape switch
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

            if (string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Ưu tiên kiểu tóc nam tính, dễ chăm sóc và giữ form tốt khi đi làm hoặc sinh hoạt thường ngày.");
            }
            else if (string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Ưu tiên kiểu tóc mềm mại, tinh tế và giúp tổng thể khuôn mặt cân đối hơn.");
            }

            if (string.Equals(ageGroup, "Dưới 18", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Bạn có thể cân nhắc kiểu trẻ trung, nhiều texture và thể hiện phong cách cá nhân.");
            }
            else if (string.Equals(ageGroup, "18-30", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Phong cách hiện đại và năng động sẽ phù hợp với nhóm tuổi này.");
            }
            else if (string.Equals(ageGroup, "31-45", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Giữ kiểu vừa hiện đại vừa dễ duy trì trong công việc và sinh hoạt hằng ngày.");
            }
            else if (string.Equals(ageGroup, "46+", StringComparison.OrdinalIgnoreCase))
            {
                tips.Add("Nên ưu tiên kiểu đơn giản, mềm mại và dễ chỉnh để phù hợp với nhịp sống thường ngày.");
            }

            return tips;
        }

        private static List<Hairstyle> ApplyProfileFilters(List<Hairstyle> suggestions, string? gender, string? ageGroup)
        {
            var genderFiltered = FilterSuggestionsByGender(suggestions, gender);
            if (!genderFiltered.Any())
            {
                genderFiltered = suggestions;
            }

            var ageFiltered = FilterSuggestionsByAge(genderFiltered, ageGroup);
            return ageFiltered.Any() ? ageFiltered : genderFiltered;
        }

        private static List<Hairstyle> FilterSuggestionsByGender(List<Hairstyle> suggestions, string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
            {
                return suggestions;
            }

            var filteredSuggestions = suggestions
                .Where(item => ShouldIncludeSuggestionForGender(item.StyleName, gender))
                .ToList();

            return filteredSuggestions.Any() ? filteredSuggestions : suggestions;
        }

        private static List<Hairstyle> FilterSuggestionsByAge(List<Hairstyle> suggestions, string? ageGroup)
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return suggestions;
            }

            var filteredSuggestions = suggestions
                .Where(item => ShouldIncludeSuggestionForAge(item.StyleName, ageGroup))
                .ToList();

            return filteredSuggestions.Any() ? filteredSuggestions : suggestions;
        }

        private static bool ShouldIncludeSuggestionForGender(string? styleName, string gender)
        {
            var normalizedStyle = NormalizeStyleKey(styleName);

            if (string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase))
            {
                return IsMaleStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            if (string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase))
            {
                return IsFemaleStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            return true;
        }

        private static bool ShouldIncludeSuggestionForAge(string? styleName, string ageGroup)
        {
            var normalizedStyle = NormalizeStyleKey(styleName);

            if (string.Equals(ageGroup, "Dưới 18", StringComparison.OrdinalIgnoreCase))
            {
                return IsYouthfulStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            if (string.Equals(ageGroup, "18-30", StringComparison.OrdinalIgnoreCase))
            {
                return IsModernStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            if (string.Equals(ageGroup, "31-45", StringComparison.OrdinalIgnoreCase))
            {
                return IsBalancedStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            if (string.Equals(ageGroup, "46+", StringComparison.OrdinalIgnoreCase))
            {
                return IsMatureStyle(normalizedStyle) || IsUnisexStyle(normalizedStyle);
            }

            return true;
        }

        private static bool IsMaleStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("fade")
                || normalizedStyle.Contains("cut")
                || normalizedStyle.Contains("pompadour")
                || normalizedStyle.Contains("quiff")
                || normalizedStyle.Contains("slick")
                || normalizedStyle.Contains("undercut")
                || normalizedStyle.Contains("ivy")
                || normalizedStyle.Contains("crew")
                || normalizedStyle.Contains("mullet")
                || normalizedStyle.Contains("buzz");
        }

        private static bool IsFemaleStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("wave")
                || normalizedStyle.Contains("fringe")
                || normalizedStyle.Contains("layer")
                || normalizedStyle.Contains("curl")
                || normalizedStyle.Contains("bob")
                || normalizedStyle.Contains("loose")
                || normalizedStyle.Contains("side-swept")
                || normalizedStyle.Contains("middle-part")
                || normalizedStyle.Contains("long");
        }

        private static bool IsUnisexStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("part")
                || normalizedStyle.Contains("crop")
                || normalizedStyle.Contains("swept");
        }

        private static bool IsYouthfulStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("wave")
                || normalizedStyle.Contains("fringe")
                || normalizedStyle.Contains("quiff")
                || normalizedStyle.Contains("pompadour")
                || normalizedStyle.Contains("crop")
                || normalizedStyle.Contains("loose")
                || normalizedStyle.Contains("textured");
        }

        private static bool IsModernStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("fade")
                || normalizedStyle.Contains("quiff")
                || normalizedStyle.Contains("layer")
                || normalizedStyle.Contains("wave")
                || normalizedStyle.Contains("fringe")
                || normalizedStyle.Contains("part")
                || normalizedStyle.Contains("swept")
                || normalizedStyle.Contains("crop");
        }

        private static bool IsBalancedStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("cut")
                || normalizedStyle.Contains("part")
                || normalizedStyle.Contains("layer")
                || normalizedStyle.Contains("swept")
                || normalizedStyle.Contains("ivy")
                || normalizedStyle.Contains("slick")
                || normalizedStyle.Contains("side");
        }

        private static bool IsMatureStyle(string normalizedStyle)
        {
            return normalizedStyle.Contains("cut")
                || normalizedStyle.Contains("part")
                || normalizedStyle.Contains("layer")
                || normalizedStyle.Contains("swept")
                || normalizedStyle.Contains("crew")
                || normalizedStyle.Contains("classic")
                || normalizedStyle.Contains("slick");
        }

        private static List<Hairstyle> GetFallbackSuggestions(string shape, string? gender)
        {
            if (string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase))
            {
                return GetFemaleFallbackSuggestions(shape);
            }

            return GetMaleFallbackSuggestions(shape);
        }

        private static List<Hairstyle> GetMaleFallbackSuggestions(string shape)
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

        private static List<Hairstyle> GetFemaleFallbackSuggestions(string shape)
        {
            return shape switch
            {
                "Tròn" => new List<Hairstyle>
                {
                    new() { FaceShape = "Tròn", StyleName = "Loose Wave", Description = "Tạo độ mềm mại và giúp khuôn mặt tròn trông cân đối hơn.", ImageUrl = "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Tròn", StyleName = "Side Swept", Description = "Tạo đường mái lệch nhẹ để cân chỉnh gương mặt tròn.", ImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Tròn", StyleName = "Fringe Layer", Description = "Phần mái mềm giúp khuôn mặt tròn hòa hợp và nữ tính hơn.", ImageUrl = "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80" }
                },
                "Vuông" => new List<Hairstyle>
                {
                    new() { FaceShape = "Vuông", StyleName = "Layer Side Sweep", Description = "Mái chéo mềm giúp giảm cảm giác góc cạnh cho khuôn mặt vuông.", ImageUrl = "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Vuông", StyleName = "Loose Wave", Description = "Tạo độ mềm mại và giúp tổng thể khuôn mặt nữ tính hơn.", ImageUrl = "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Vuông", StyleName = "Fringe Layer", Description = "Giữ mái nhẹ để cân bằng đường nét và tạo cảm giác tinh tế.", ImageUrl = "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80" }
                },
                "Dài" => new List<Hairstyle>
                {
                    new() { FaceShape = "Dài", StyleName = "Middle Part", Description = "Chia ngôi giữa tạo cảm giác cân đối và thư sinh cho khuôn mặt dài.", ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Dài", StyleName = "Fringe Layer", Description = "Mái mềm tạo điểm nhấn và giúp làm ngắn cảm giác dài của khuôn mặt.", ImageUrl = "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Dài", StyleName = "Side Swept", Description = "Tạo hiệu ứng xoè nhẹ để khuôn mặt dài trở nên cân đối hơn.", ImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80" }
                },
                _ => new List<Hairstyle>
                {
                    new() { FaceShape = "Trái xoan", StyleName = "Loose Wave", Description = "Uốn nhẹ tạo chiều sâu và mang phong cách nữ tính hiện đại.", ImageUrl = "https://images.unsplash.com/photo-1519345182560-3f2917c472ef?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Trái xoan", StyleName = "Fringe Layer", Description = "Mái nhuyễn tạo độ dịu dàng và cân đối với đường nét khuôn mặt.", ImageUrl = "https://images.unsplash.com/photo-1519895609939-2795e7b7d25b?auto=format&fit=crop&w=900&q=80" },
                    new() { FaceShape = "Trái xoan", StyleName = "Side Swept", Description = "Dáng tóc mềm mại giúp khối khuôn mặt thêm thanh thoát.", ImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=900&q=80" }
                }
            };
        }
    }
}
