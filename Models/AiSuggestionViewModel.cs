using System.Collections.Generic;

namespace SalonHair.Models
{
    public class AiSuggestionViewModel
    {
        public string FaceShape { get; set; } = string.Empty;

        public string? Gender { get; set; }

        public string? AgeGroup { get; set; }

        public double Confidence { get; set; }

        public string ConfidenceLabel { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public List<string> StylingTips { get; set; } = new();

        public List<Hairstyle> Suggestions { get; set; } = new();

        public bool UsedFallbackData { get; set; }
    }
}
