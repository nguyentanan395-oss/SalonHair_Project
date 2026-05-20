namespace SalonHair.Models
{
    public class AiFeedbackTrainResult
    {
        public bool ModelCreated { get; set; }

        public int TotalSamples { get; set; }

        public int DistinctShapes { get; set; }

        public string? ModelPath { get; set; }

        public string? ModelVersion { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
