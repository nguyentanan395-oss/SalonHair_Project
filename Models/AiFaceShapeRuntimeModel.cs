using System;
using System.Collections.Generic;

namespace SalonHair.Models
{
    public class AiFaceShapeRuntimeModel
    {
        public string ModelVersion { get; set; } = "feedback-centroid-v1";

        public DateTime TrainedAtUtc { get; set; }

        public int TotalSamples { get; set; }

        public List<string> FeatureOrder { get; set; } = new();

        public List<double> GlobalMeans { get; set; } = new();

        public List<double> GlobalStdDevs { get; set; } = new();

        public List<AiFaceShapeRuntimeClass> Classes { get; set; } = new();
    }

    public class AiFaceShapeRuntimeClass
    {
        public string Shape { get; set; } = string.Empty;

        public int SampleCount { get; set; }

        public List<double> Centroid { get; set; } = new();
    }
}
