namespace SalonHair.Models
{
    public class AiFeedbackImportResult
    {
        public int ImportedSamples { get; set; }

        public int SkippedSamples { get; set; }

        public int ImportedSnapshots { get; set; }

        public int TotalSamples { get; set; }
    }
}
