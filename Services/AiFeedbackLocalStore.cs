using SalonHair.Models;

namespace SalonHair.Services
{
    public class AiFeedbackLocalStore
    {
        public async Task<AiFeedbackDatasetSample> SaveAsync(AiFeedbackSaveRequest request, string predictedShape, string correctedShape, CancellationToken cancellationToken)
        {
            return new AiFeedbackDatasetSample
            {
                IsPredictionAccepted = predictedShape == correctedShape,
                SampleId = Guid.NewGuid().ToString(),
                CorrectedShape = correctedShape
            };
        }

        public async Task<AiFeedbackDatasetStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            return new AiFeedbackDatasetStats { TotalSamples = 0 };
        }

        public async Task<(byte[] Content, string FileName)> ExportAsync(CancellationToken cancellationToken)
        {
            return (Array.Empty<byte>(), "dataset.zip");
        }

        public async Task<AiFeedbackImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken)
        {
            return new AiFeedbackImportResult();
        }

        public async Task<object?> LoadModelAsync(CancellationToken cancellationToken)
        {
            return null;
        }
    }
}
