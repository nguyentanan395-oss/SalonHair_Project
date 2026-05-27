using SalonHair.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SalonHair.Services
{
    public class AiFeedbackLocalStore
    {
        private readonly List<AiFeedbackDatasetSample> _samples = new();
        private readonly object _lock = new();

        public async Task<AiFeedbackDatasetSample> SaveAsync(AiFeedbackSaveRequest request, string predictedShape, string correctedShape, CancellationToken cancellationToken)
        {
            var sample = new AiFeedbackDatasetSample
            {
                IsPredictionAccepted = string.Equals(predictedShape, correctedShape, StringComparison.OrdinalIgnoreCase),
                SampleId = Guid.NewGuid().ToString(),
                PredictedShape = predictedShape,
                CorrectedShape = correctedShape,
                Confidence = request.Confidence,
                FaceLengthRatio = request.FaceLengthRatio,
                ForeheadWidthRatio = request.ForeheadWidthRatio,
                JawWidthRatio = request.JawWidthRatio,
                ForeheadJawDelta = request.ForeheadJawDelta,
                DetectionSource = request.DetectionSource ?? string.Empty,
                ModelVersion = request.ModelVersion ?? string.Empty,
                ClientSessionId = request.ClientSessionId,
                LandmarksJson = request.LandmarksJson,
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (_lock)
            {
                _samples.Add(sample);
            }

            return sample;
        }

        public async Task<AiFeedbackDatasetStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return new AiFeedbackDatasetStats { TotalSamples = _samples.Count };
            }
        }

        public async Task<int> GetDistinctShapesCountAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _samples
                    .Select(sample => sample.CorrectedShape)
                    .Where(shape => !string.IsNullOrWhiteSpace(shape))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
            }
        }

        public async Task<(byte[] Content, string FileName)> ExportAsync(CancellationToken cancellationToken)
        {
            return (Array.Empty<byte>(), "dataset.zip");
        }

        public async Task<AiFeedbackImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken)
        {
            return new AiFeedbackImportResult();
        }

        public async Task<AiFaceShapeRuntimeModel?> LoadModelAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (!_samples.Any())
                {
                    return null;
                }

                var classes = _samples
                    .GroupBy(sample => sample.CorrectedShape)
                    .Select(group => new AiFaceShapeRuntimeClass
                    {
                        Shape = group.Key,
                        SampleCount = group.Count(),
                        Centroid = new List<double> { group.Count() }
                    })
                    .ToList();

                return new AiFaceShapeRuntimeModel
                {
                    ModelVersion = "feedback-model-v1",
                    TrainedAtUtc = DateTime.UtcNow,
                    TotalSamples = _samples.Count,
                    FeatureOrder = new List<string> { "FaceLengthRatio", "ForeheadWidthRatio", "JawWidthRatio", "ForeheadJawDelta" },
                    GlobalMeans = new List<double>(),
                    GlobalStdDevs = new List<double>(),
                    Classes = classes
                };
            }
        }
    }
}
