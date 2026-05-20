using System;

namespace SalonHair.Models
{
    public class AiFeedbackDatasetSample
    {
        public string SampleId { get; set; } = string.Empty;

        public string PredictedShape { get; set; } = string.Empty;

        public string CorrectedShape { get; set; } = string.Empty;

        public bool IsPredictionAccepted { get; set; }

        public double Confidence { get; set; }

        public double FaceLengthRatio { get; set; }

        public double ForeheadWidthRatio { get; set; }

        public double JawWidthRatio { get; set; }

        public double ForeheadJawDelta { get; set; }

        public string DetectionSource { get; set; } = string.Empty;

        public string ModelVersion { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;

        public string? ClientSessionId { get; set; }

        public string? SnapshotContentType { get; set; }

        public string? SnapshotRelativePath { get; set; }

        public string? LandmarksJson { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
