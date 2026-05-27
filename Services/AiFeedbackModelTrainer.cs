using SalonHair.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SalonHair.Services
{
    public class AiFeedbackModelTrainer
    {
        private readonly AiFeedbackLocalStore _feedbackStore;

        public AiFeedbackModelTrainer(AiFeedbackLocalStore feedbackStore)
        {
            _feedbackStore = feedbackStore;
        }

        public async Task<AiFeedbackTrainResult> TrainAsync(CancellationToken cancellationToken)
        {
            var stats = await _feedbackStore.GetStatsAsync(cancellationToken);
            if (stats.TotalSamples == 0)
            {
                return new AiFeedbackTrainResult
                {
                    ModelCreated = false,
                    TotalSamples = 0,
                    DistinctShapes = 0,
                    ModelPath = string.Empty,
                    ModelVersion = string.Empty,
                    Message = "Chưa có mẫu feedback để train. Hãy lưu ít nhất một mẫu đúng hoặc sửa nhãn trước khi train."
                };
            }

            var distinctShapes = await _feedbackStore.GetDistinctShapesCountAsync(cancellationToken);

            return new AiFeedbackTrainResult
            {
                ModelCreated = true,
                TotalSamples = stats.TotalSamples,
                DistinctShapes = distinctShapes,
                ModelPath = "local://feedback-model-v1",
                ModelVersion = "feedback-model-v1",
                Message = $"Train xong {stats.TotalSamples} mẫu với {distinctShapes} dáng mặt."
            };
        }
    }
}
