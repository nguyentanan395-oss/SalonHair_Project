using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SalonHair.Models;
namespace SalonHair.Services
{
   public class FaceFeatureExtractor
{
    public FaceTrainingData Extract(List<FaceLandmark> points, string label)
    {
        return new FaceTrainingData
        {
            FaceWidth = Distance(points[234], points[454]),

            FaceHeight = Distance(points[10], points[152]),

            JawWidth = Distance(points[172], points[397]),

            ForeheadWidth = Distance(points[54], points[284]),

            Label = label
        };
    }

    private float Distance(FaceLandmark a, FaceLandmark b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
}
