namespace SalonHair.Models
{
    public class AiFeedbackImportResult
    {
        public int ImportedSamples { get; set; }

        public int DuplicateSamples { get; set; }

        public bool ModelLoaded { get; set; }

        public string Message { get; set; }
            = string.Empty;
        public int SkippedSamples { get; set; }

        public int ImportedSnapshots { get; set; }

        public int TotalSamples { get; set; }
    }
}
