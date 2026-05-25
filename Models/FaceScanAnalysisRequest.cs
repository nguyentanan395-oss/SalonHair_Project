namespace SalonHair.Models
{
    public class FaceScanAnalysisRequest
    {
        public string? DetectedShape { get; set; }

        public string? Gender { get; set; }

        public string? AgeGroup { get; set; }

        public double Confidence { get; set; }

        public double FaceLengthRatio { get; set; }

        public double ForeheadWidthRatio { get; set; }

        public double JawWidthRatio { get; set; }

        public double ForeheadJawDelta { get; set; }
    }
}
