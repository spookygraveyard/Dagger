using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.UI;

namespace Dagger
{
    public class ECData
    {
        public float[,] downSampledData;
        public Vector2[,] workingData;
        public byte[] cScanBytes;
        public int pointCount;
        public int channelCount;

        public List<ECGroup> groups;
        public int groupInd = 0;
        public int groupChoice = 0;

        public Vector2[,] baseData;
        public int[] convexHull;
        public int measureInd1 = -1, measureInd2 = -1;
        public int mxRInd1 = -1, mxRInd2 = -1;
        public double measureAmplitude;
        public int measurementPhase;
        public string measurementType = "Vpp";
        public string[] freqNames;
        public bool downSampled = false;
        public bool verticalDisplay = true;

        private int[] rotationsInDegrees;
        private Vector2[] nullPoints;
        private float[] scales;
        private double[] measureScales;

        public ECData(Vector2[,] dataIn, string[] freqNamesIn, List<ECGroup> groupsIn, int dataChanCount)
        {
            baseData = dataIn;
            pointCount = dataIn.GetLength(0);
            channelCount = dataChanCount;
            workingData = new Vector2[pointCount, channelCount];
            scales = new float[channelCount];
            nullPoints = new Vector2[channelCount];
            rotationsInDegrees = new int[channelCount];
            for (int i = 0; i < channelCount; i++) { scales[i] = 0.1F; }
            nullPoints.Initialize();
            rotationsInDegrees.Initialize();
            freqNames = freqNamesIn;
            groups = groupsIn;
        }

        public ECData(Vector2[,] dataIn, string[] freqNamesIn, List<ECGroup> groupsIn, int[] rotationsIn, double[] measureScalesIn)
        {
            baseData = dataIn;
            pointCount = dataIn.GetLength(0);
            channelCount = dataIn.GetLength(1);
            workingData = new Vector2[pointCount, channelCount];
            scales = new float[channelCount];
            nullPoints = new Vector2[channelCount];
            rotationsInDegrees = rotationsIn;
            measureScales = measureScalesIn;
            for (int i = 0; i < channelCount; i++) { scales[i] = 0.1F; }
            nullPoints.Initialize();
            rotationsInDegrees.Initialize();
            freqNames = freqNamesIn;
            groups = groupsIn;
        }

        public void SetRotations(int[] rotsIn)
        {
            rotationsInDegrees = rotsIn;
        }

        public void Rotate(int channel, int degrees)
        {
            rotationsInDegrees[channel] = degrees;
            UpdatePoints(channel);            
        }

        public void UpdateNullPoint(int channel, Vector2 nullPoint)
        {
            nullPoints[channel] = nullPoint;
            UpdatePoints(channel);
        }

        public void UpdateNullPointIndex(int channel, int index)
        {
            nullPoints[channel] = baseData[index, channel];
            UpdatePoints(channel);
        }

        public void Scale(int channel, float scale)
        {
            scales[channel] = scale;
            UpdatePoints(channel);
        }
        public void UpdatePoints(int channel)
        {
            Matrix3x2 translateMatrix = Matrix3x2.CreateTranslation(-nullPoints[channel]);
            Matrix3x2 rotMatrix = Matrix3x2.CreateRotation((float)(rotationsInDegrees[channel] * Math.PI / 180));
            Matrix3x2 scaleMatrix = Matrix3x2.CreateScale(scales[channel]);

            // Combine matrices (T * R * S for composition)
            Matrix3x2 compMatrix = translateMatrix * rotMatrix * scaleMatrix;

            for (int i = 0; i < baseData.GetLength(0); i++)
                workingData[i, channel] = Vector2.Transform(baseData[i, channel], compMatrix);
        }

        public int GetRotation(int channel)
        {
            return rotationsInDegrees[channel];
        }

        public float GetScale(int channel)
        {
            return scales[channel];
        }

        public Vector2 GetNullPoint(int channel)
        {
            return nullPoints[channel];
        }

        public void ResizeData(int pixelIn)
        {
            if (downSampled = (pointCount / pixelIn) >= 3)
            {
                int bucketSize = pointCount / pixelIn;
                if (pointCount % pixelIn != 0)
                    bucketSize += 1;

                int subChanCount = groups[0].Chans.Count;
                downSampledData = new float[pixelIn, subChanCount];
                for (int j = 0; j < subChanCount; j++)
                {
                    int curChan = groups[groupInd].Chans[j];
                    float maxVal = float.MinValue;
                    //float minVal = float.MaxValue;
                    int bucketCounter = 0;
                    for (int i = 0; i < baseData.GetLength(0); i++)
                    {
                        if (baseData[i, curChan].Y > maxVal)
                            maxVal = baseData[i, curChan].Y;
                        //if (baseData[i, curChan].Y < minVal)
                            //minVal = baseData[i, curChan].Y;

                        bucketCounter++;
                        if (bucketCounter >= bucketSize)
                        {
                            downSampledData[i / bucketSize, j] = maxVal;
                            bucketCounter = 0;
                            maxVal = float.MinValue;
                            //minVal = float.MaxValue;
                        }
                    }
                }
            }
        }

        public void UpdateLineIndex(int indIn)
        {
            if (downSampledData == null)
                return;
            int width = downSampledData.GetLength(1);
            float[] subs = new float[width]; 
            for (int i = 0; i < width; i++)
                subs[i] = baseData[indIn, groups[groupChoice].Chans[i]].Y;

            for (int i = 0; i < downSampledData.GetLength(0); i++)
            {
                for (int j = 0; j < width; j++)
                    downSampledData[i, j] -= subs[j];
            }
            UpdateCScanBytes();
        }

        public void UpdateCScanBytes()
        {
            //if (downSampledData == null)
            //    return;

            //int bytesPerPixel = 4;
            //cScanBytes = new byte[bytesPerPixel * downSampledData.Length];
            //int ind = 0;
            //for (int i = 0; i < downSampledData.GetLength(0); i++)
            //{
            //    for (int j = 0; j < downSampledData.GetLength(1); j++)
            //    {
            //        int index = (int)(downSampledData[i, j] / 5.0F + 2.5F);
            //        if (index > rainbow.Length - 1)
            //            index = rainbow.Length - 1;
            //        if (index < 0)
            //            index = 0;
            //        Color a = rainbow[index];
            //        // DirectXPixelFormat.B8G8R8A8UintNormalized
            //        // Doing this byte-by-byte is probably not the best
            //        // Maybe in future try to have the colours in the correct format
            //        // Then make an array of thoses colours, then convert the colour
            //        // array into a byte array at the end.
            //        cScanBytes[ind]     = a.B;
            //        cScanBytes[ind + 1] = a.G;
            //        cScanBytes[ind + 2] = a.R;
            //        cScanBytes[ind + 3] = a.A;
            //        ind += bytesPerPixel;
            //    }
            //}
        }

        public static double Cross(Vector2 O, Vector2 A, Vector2 B)
        {
            // Used in the monotone chain algorithm
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        public static double DistSquared(Vector2 A, Vector2 B)
        {
            return (A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y);
        }

        public static int GetECAngle(Vector2 A, Vector2 B)
        {
            // Clockwise, horizontal left --> 0 degrees
            double rotInRads = Math.Atan2(A.Y - B.Y, A.X - B.X);
            if (rotInRads < 0)
                rotInRads = -rotInRads + Math.PI;
            else
                rotInRads = Math.PI - rotInRads;
            return 360 - (int)(rotInRads * 180.0 / Math.PI);
        }

        public void InvalidateMeasurement()
        {
            convexHull = null;
            measureInd1 = 0;
            measureInd2 = 0;
        }

        public void GetVppBruteForce(int startInd, int endInd, int channel)
        {
            // For testing/benchmarking
            // Performance scales poorly with larger sets of data
            Vector2[] pointsIn = new Vector2[endInd - startInd];
            double biggestDist = -1.0;
            for (int i = 0; i < endInd - startInd; i++)
                pointsIn[i] = workingData[i + startInd, channel];

            for (int i = 0; i < pointsIn.GetLength(0) - 1; i++)
            {
                for (int j = i + 1; j < pointsIn.GetLength(0); j++)
                {
                    double d = DistSquared(pointsIn[i], pointsIn[j]);
                    if (d > biggestDist)
                        biggestDist = d;
                }
            }
        }

        public void GetVmx(int startInd, int endInd, int channel)
        {
            // Vmx -> Vertical Maximum
            Vector2[] pointsIn = new Vector2[endInd - startInd];
            for (int i = 0; i < endInd - startInd; i++)
                pointsIn[i] = workingData[i + startInd, channel];

            float max = float.MinValue;
            float min = float.MaxValue;
            int maxInd = 0, minInd = 0;

            for (int i = 0; i < pointsIn.Length; i++)
            {
                if (pointsIn[i].Y > max)
                {
                    max = pointsIn[i].Y;
                    maxInd = i;
                }
                if (pointsIn[i].Y < min)
                {
                    min = pointsIn[i].Y;
                    minInd = i;
                }
                measureAmplitude = max - min;
                measureInd1 = startInd + maxInd;
                measureInd2 = startInd + minInd;
            }
            measurementType = "Vmx";
        }

        public int IncrementGroupInd()
        {
            groupInd++;
            if (groupInd >= groups[groupChoice].Chans.Count)
                groupInd = 0;
            return groups[groupChoice].Chans[groupInd];
        }

        public bool UpdateGroup(int chanIn)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = 0; j < groups[i].Chans.Count; j++)
                {
                    if (groups[i].Chans[j] == chanIn)
                    {
                        groupChoice = i;
                        groupInd = j;
                        return true;
                    }
                }
            }
            return false;
        }

        public void GetHmx(int startInd, int endInd, int channel)
        {
            // Hmx -> Horizontal Maximum
            Vector2[] pointsIn = new Vector2[endInd - startInd];
            for (int i = 0; i < endInd - startInd; i++)
                pointsIn[i] = workingData[i + startInd, channel];

            float max = float.MinValue;
            float min = float.MaxValue;
            int maxInd = 0, minInd = 0;

            for (int i = 0; i < pointsIn.Length; i++)
            {
                if (pointsIn[i].X > max)
                {
                    max = pointsIn[i].X;
                    maxInd = i;
                }
                if (pointsIn[i].X < min)
                {
                    min = pointsIn[i].X;
                    minInd = i;
                }
                measureAmplitude = max - min;
                measureInd1 = startInd + maxInd;
                measureInd2 = startInd + minInd;
            }
            measurementType = "Hmx";
        }

        public void GetVpp(int startInd, int endInd, int channel)
        {
            Vector2[] pointsIn = new Vector2[endInd - startInd];
            Vector2[] pointsInRem = new Vector2[endInd - startInd];
            for (int i = 0; i < endInd - startInd; i++)
            {
                pointsIn[i] = workingData[i + startInd, channel];
                pointsInRem[i] = workingData[i + startInd, channel];
            }

            //==========
            // Fast Vpp
            //==========

            // Get points on convex hull using Andrew's monotone chain algorithm
            int n = pointsIn.Length;
            int k = 0;
            int mid;
            Vector2[] H = new Vector2[2 * n];

            //var sortedP = pointsIn.OrderBy(p => p.X).ThenBy(p => p.Y);
            //pointsIn = sortedP.ToArray();
            Vec2Compare comp = new Vec2Compare();   // I think this is faster than the LINQ above
            Array.Sort(pointsIn, comp);

            // Lower Hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross(H[k - 2], H[k - 1], pointsIn[i]) <= 0)
                    k--;
                H[k++] = pointsIn[i];
            }
            mid = k;    // Remember upper from lower hull (would be used in rotating caliper's algo.)
            // Upper Hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(H[k - 2], H[k - 1], pointsIn[i]) <= 0)
                    k--;
                H[k++] = pointsIn[i];
            }
            if (k > 1)
            {
                Vector2[] trimmed = new Vector2[k - 1];
                for (int i = 0; i < k - 1; i++)
                    trimmed[i] = H[i];
                H = trimmed;
            }
            convexHull = new int[H.Length];
            for(int i = 0; i < H.Length; i++)
                convexHull[i] = startInd + Array.IndexOf(pointsInRem, H[i]);

            // Brute force the points on the convex hull (for now)
            // There are O(n) ways of doing this, but it shouldn't matter much
            Vector2 tempPoint1 = new Vector2(), tempPoint2 = new Vector2();
            double biggestDist = -1.0;
            for (int i = 0; i < H.GetLength(0) - 1; i++)
            {
                for (int j = i + 1; j < H.GetLength(0); j++)
                {
                    double d = DistSquared(H[i], H[j]);
                    if (d > biggestDist)
                    {
                        biggestDist = d;
                        tempPoint1 = H[i];
                        tempPoint2 = H[j];
                    }
                }
            }
            //measurePoint1 = tempPoint1;
            //measurePoint2 = tempPoint2;
            measureInd1 = startInd + Array.IndexOf(pointsInRem, tempPoint1);
            measureInd2 = startInd + Array.IndexOf(pointsInRem, tempPoint2);
            measureAmplitude = Math.Sqrt(biggestDist);
            if (measureInd1 < measureInd2)
                measurementPhase = GetECAngle(tempPoint1, tempPoint2);
            else
                measurementPhase = GetECAngle(tempPoint2, tempPoint1);
            measurementType = "Vpp";
            // Find the maximum distance with the rotating calipers algorithm        
            //double longest = 0;
            //int index1 = -1, index2 = -1;
            //int bot = 0;
            //int top = H.Length - 1;
            //
            //// First set of points
            //double ds = DistSquared(H[bot], H[top]);
            //longest = ds;
            //index1 = bot;
            //index2 = top;
            //
            //while (bot < mid || top >= mid)
            //{
            //    if (bot == mid)
            //        top--;
            //    else if (top == mid + 1)
            //        bot++;
            //    else if (
            //        (H[bot + 1].Y - H[bot].Y) * (H[top].X - H[top - 1].X) >
            //        (H[bot + 1].X - H[bot].X) * (H[top].Y - H[top - 1].Y))
            //        bot++;
            //    else
            //        top--;
            //
            //    ds = DistSquared(H[bot], H[top]);
            //    if (ds > longest)
            //    {
            //        longest = ds;
            //        index1 = bot;
            //        index2 = top;
            //    }
            //}

            //measurePoint1 = H[index1];
            //measurePoint2 = H[index2];
            //return new MeasurementResult {
            //    MeasurementType = "Vpp",
            //    Amplitude = longest
            //};
        }

        public void GetMxR(int startInd, int endInd, int channel)
        {
            // MxR -> Max Rate
            // MxR Needs the Vpp points, so we calculate those first

            // Do Vpp
            Vector2[] pointsIn = new Vector2[endInd - startInd];
            Vector2[] pointsInRem = new Vector2[endInd - startInd];
            for (int i = 0; i < endInd - startInd; i++)
            {
                pointsIn[i] = workingData[i + startInd, channel];
                pointsInRem[i] = workingData[i + startInd, channel];
            }

            // Get points on convex hull using Andrew's monotone chain algorithm
            int n = pointsIn.Length;
            int k = 0;
            int mid;
            Vector2[] H = new Vector2[2 * n];

            //var sortedP = pointsIn.OrderBy(p => p.X).ThenBy(p => p.Y);
            //pointsIn = sortedP.ToArray();
            Vec2Compare comp = new Vec2Compare();   // I think this is faster than the LINQ above
            Array.Sort(pointsIn, comp);

            // Lower Hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross(H[k - 2], H[k - 1], pointsIn[i]) <= 0)
                    k--;
                H[k++] = pointsIn[i];
            }
            mid = k;    // Remeber upper from lower hull
            // Upper Hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(H[k - 2], H[k - 1], pointsIn[i]) <= 0)
                    k--;
                H[k++] = pointsIn[i];
            }
            if (k > 1)
            {
                Vector2[] trimmed = new Vector2[k - 1];
                for (int i = 0; i < k - 1; i++)
                    trimmed[i] = H[i];
                H = trimmed;
            }
            convexHull = new int[H.Length];
            for (int i = 0; i < H.Length; i++)
                convexHull[i] = startInd + Array.IndexOf(pointsInRem, H[i]);

            // Brute force the points on the convex hull (for now)
            // There are O(n) ways of doing this, but it shouldn't matter much
            Vector2 tempPoint1 = new Vector2(), tempPoint2 = new Vector2();
            double biggestDist = -1.0;
            for (int i = 0; i < H.GetLength(0) - 1; i++)
            {
                for (int j = i + 1; j < H.GetLength(0); j++)
                {
                    double d = DistSquared(H[i], H[j]);
                    if (d > biggestDist)
                    {
                        biggestDist = d;
                        tempPoint1 = H[i];
                        tempPoint2 = H[j];
                    }
                }
            }

            measureInd1 = startInd + Array.IndexOf(pointsInRem, tempPoint1);
            measureInd2 = startInd + Array.IndexOf(pointsInRem, tempPoint2);
            measureAmplitude = Math.Sqrt(biggestDist);
            
            // Vpp points found, and amplitude calculated!
            // Now we can do the MxR part

            // MxR angle calculation
            int index1 = Math.Min(measureInd1, measureInd2) - startInd;
            int index2 = Math.Max(measureInd1, measureInd2) - startInd;

            const double TOL = 2.5; // This value taken from ADAPT's MxR algorithm
            int tail = index1, bestHead = index1 + 1, bestTail = index1;
            double angle = GetECAngle(pointsInRem[tail], pointsInRem[index1 + 1]);
            double lastAngle = angle;
            double bestLength = DistSquared(pointsInRem[tail], pointsInRem[index1 + 1]);

            for (int i = index1 + 1; i < index2; i++)
            {
                angle = GetECAngle(pointsInRem[i], pointsInRem[i + 1]);
                if (Math.Abs(angle - lastAngle) > TOL)
                {
                    if (DistSquared(pointsInRem[i], pointsInRem[tail]) > bestLength)
                    {
                        bestTail = tail;
                        bestLength = DistSquared(pointsInRem[i], pointsInRem[tail]);
                        bestHead = i;
                    }
                    tail = i;
                }
                lastAngle = angle;
            }
            measurementPhase = GetECAngle(pointsInRem[bestTail], pointsInRem[bestHead]);
            mxRInd1 = bestHead + startInd;
            mxRInd2 = bestTail + startInd;
            measurementType = "MxR";
        }

        public double[] FullVmx(int channel)
        {
            // Pretty quick (~5 ms for 32,000 points)
            UpdatePoints(channel);  // Need to have calibrated data ready (~1 ms)
            int length = pointCount / 10;
            double[] toRet = new double[length];
            const int windowSize = 100;
            for (int i = 0; i < length; i++)
            {
                int start = i * 10;
                if (start + windowSize >= pointCount)
                    start = pointCount - windowSize - 1;

                float max = float.MinValue;
                float min = float.MaxValue;

                int end = start + windowSize;
                for (int j = start; j < end; j++)
                {
                    if (workingData[j, channel].Y > max)
                        max = workingData[j, channel].Y;
                    if (workingData[j, channel].Y < min)
                        min = workingData[j, channel].Y;
                }
                toRet[i] = max - min;
            }
            return toRet;
        }
    }

    // Used to sort vectors, X, then Y
    class Vec2Compare : IComparer<Vector2>
    {
        public int Compare(Vector2 v1, Vector2 v2)
        {
            if (v1.X > v2.X)
                return 1;
            else if (v1.X == v2.X)
            {
                if (v1.Y > v2.Y)
                    return 1;
                else if (v1.Y == v2.Y)
                    return 0;
                else
                    return -1;
            }
            else
                return -1;
        }
    }

}
