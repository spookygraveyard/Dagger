using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dagger
{
    class ECCalibration
    {
        public string Name { get; set; }
        public string TechniqueID { get; set; }
        public string Material { get; set; }
        public int[] LocationSignal { get; set; }
        public int[] TargetPhases { get; set; }
        public double[] TargetAmps { get; set; }
        public string[] DeflectDirection { get; set; }
        public double PullSpeedCMperSecond { get; set; }
        public double SampleRate { get; set; }
        public int AmpFeatureIndex { get; set; }
        public int PhaseFeatureIndex { get; set; }
        public int SpanFeatureIndex { get; set; }
        public double SpanTargetHeight { get; set; }
        public List<ECCalFeature> CalFeatures { get; set; }

        /// <summary>
        /// Generates a new EC Calibration file from an existing .json file
        /// </summary>
        /// <param name="filePath">Path to the calibration .json file</param>
        public ECCalibration(string filePath)
        {

        }

        public ECCalibration()
        {

        }
    }
}
