
using Microsoft.ML;
using SalonHair.Models;

namespace SalonHair.Services
{
    public class AiFeedbackModelTrainer
    {
        private readonly AiFeedbackLocalStore _feedbackStore;
        private readonly FaceFeatureExtractor _extractor;

        public AiFeedbackModelTrainer(
            AiFeedbackLocalStore feedbackStore,
            FaceFeatureExtractor extractor)
        {
            _feedbackStore = feedbackStore;
            _extractor = extractor;
        }

        public async Task<AiFeedbackTrainResult> TrainAsync(
            CancellationToken cancellationToken)
        {
            var samples = await _feedbackStore.GetAllAsync(cancellationToken);

            if (!samples.Any())
            {
                return new AiFeedbackTrainResult
                {
                    ModelCreated = false,
                    Message = "Không có dataset để train."
                };
            }

            var trainingData = new List<FaceTrainingData>();

            foreach (var sample in samples)
            {
                if (string.IsNullOrWhiteSpace(sample.LandmarksJson))
    continue;

var landmarks =
    System.Text.Json.JsonSerializer.Deserialize<
        List<FaceLandmark>>(
            sample.LandmarksJson);

if (landmarks == null || !landmarks.Any())
    continue;

var feature = _extractor.Extract(
    landmarks,
    sample.CorrectedShape);

                trainingData.Add(feature);
            }

            var ml = new MLContext();

            IDataView dataView =
                ml.Data.LoadFromEnumerable(trainingData);

            var pipeline =
                ml.Transforms.Conversion.MapValueToKey("Label")
                .Append(
                    ml.Transforms.Concatenate(
                        "Features",
                        nameof(FaceTrainingData.FaceWidth),
                        nameof(FaceTrainingData.FaceHeight),
                        nameof(FaceTrainingData.JawWidth),
                        nameof(FaceTrainingData.ForeheadWidth)))
                .Append(
                    ml.MulticlassClassification.Trainers
                    .SdcaMaximumEntropy())
                .Append(
                    ml.Transforms.Conversion
                    .MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(dataView);

            Directory.CreateDirectory("Models");

            var modelPath = Path.Combine(
                "Models",
                "face-model.zip");

            ml.Model.Save(model, dataView.Schema, modelPath);

            return new AiFeedbackTrainResult
            {
                ModelCreated = true,
                TotalSamples = trainingData.Count,
                DistinctShapes =
                    trainingData.Select(x => x.Label)
                    .Distinct()
                    .Count(),

                ModelPath = modelPath,

                ModelVersion =
                    $"face-model-{DateTime.UtcNow:yyyyMMddHHmmss}",

                Message =
                    $"Train AI thành công với {trainingData.Count} mẫu."
            };
        }
    }
}

