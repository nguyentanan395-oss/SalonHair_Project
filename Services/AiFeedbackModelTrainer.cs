using SalonHair.Models;

namespace SalonHair.Services
{
    public class AiFeedbackModelTrainer
    {
        public async Task<AiFeedbackTrainResult> TrainAsync(CancellationToken cancellationToken)
        {
            return new AiFeedbackTrainResult
            {
                ModelCreated = false,
                TotalSamples = 0,
                DistinctShapes = 0,
                ModelPath = string.Empty,
                ModelVersion = string.Empty,
                Message = "Mock implementation because the original file was missing."
            };
        }
    }
}
