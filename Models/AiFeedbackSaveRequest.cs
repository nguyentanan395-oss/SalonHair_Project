namespace SalonHair.Models
{
    public class AiFeedbackSaveRequest
    {
        public string? PredictedShape { get; set; }

        public string? CorrectedShape { get; set; }

        public double Confidence { get; set; }

        public double FaceLengthRatio { get; set; }

        public double ForeheadWidthRatio { get; set; }

        public double JawWidthRatio { get; set; }

        public double ForeheadJawDelta { get; set; }

        public string? DetectionSource { get; set; }

        public string? ModelVersion { get; set; }

        public string? ClientSessionId { get; set; }

        public string? SnapshotDataUrl { get; set; }

        public string? LandmarksJson { get; set; }
    }
}
