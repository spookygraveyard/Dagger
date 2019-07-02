using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace Dagger
{
    class ECDataConverter
    {
        byte[] rawData;
        public string probeType;
        public int chanCount;

        public ECDataConverter(byte[] dataIn)
        {
            rawData = dataIn;
        }

        public ECData ReturnData()
        {
            // Convert the Zetec data file
            // The data length is a 32-bit integer after 8 bytes of the second 32-byte header
            byte[] temp = new byte[4];
            temp[0] = rawData[43];
            temp[1] = rawData[42];
            temp[2] = rawData[41];
            temp[3] = rawData[40];
            int dataLength = BitConverter.ToInt32(temp, 0);

            // Now that we know the data length, we can jump over it, and get to the XML stuff
            // 72 is 32 + 32 + 8 so its skiping the first two headers, all the data, then 8 bytes into the XML header
            // ...which gives us the length of XML data 
            temp[0] = rawData[75 + dataLength];
            temp[1] = rawData[74 + dataLength];
            temp[2] = rawData[73 + dataLength];
            temp[3] = rawData[72 + dataLength];
            int xmlLength = BitConverter.ToInt32(temp, 0);

            // The 22 bytes are the <xml version... tag at the start (which we skip)
            // 96 is just 3 headers worth (3 * 32)
            XElement root = XElement.Parse(Encoding.UTF8.GetString(rawData, 96 + 22 + dataLength, xmlLength - 22));
            
            // Get the important elements
            XElement typeXElement = root.Descendants("probe_type").FirstOrDefault();
            XElement chanCountXElement = root.Descendants("num_raw_chans").FirstOrDefault();
            XElement displaySetsXElement = root.Descendants("displaysets").FirstOrDefault();

            // Frequency names for each channel
            List<string> freqNames = new List<string>();
            foreach (XElement el in root.Descendants("HWChannel"))
                freqNames.Add(el.Descendants("frequency_in_khz").FirstOrDefault().Value);

            List<int> rots = new List<int>();
            List<double> measureScales = new List<double>();
            // Default calibration for each channel
            foreach (XElement el in root.Descendants("AnalysisChannel"))
            {
                rots.Add(int.Parse(el.Descendants("rotation").FirstOrDefault().Value));
                measureScales.Add(double.Parse(el.Descendants("xscale").FirstOrDefault().Value));
            }

            // Raw Channels
            int.TryParse(chanCountXElement.Value, out chanCount);
            chanCount--;
            // Probe type
            probeType = typeXElement.Value;
            // Total Channels
            int bytesPer;
            int.TryParse(root.Descendants("bytes_per_slice").FirstOrDefault().Value, out bytesPer);
            int totalChans = bytesPer / 4;
            // Data points (per channel)
            int dataPointsPerChan = dataLength / bytesPer;

            // Parse the groups (display sets)
            List<ECGroup> groups = new List<ECGroup>();

            foreach (XElement el in displaySetsXElement.Descendants("DisplaySet"))
            {
                string[] chans = el.Descendants("channels").FirstOrDefault().Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int[] chanNums = new int[chans.Length];
                int[] chanPositions = new int[chans.Length];
                for (int i = 0; i < chans.Length; i++)
                {
                    chanNums[i] = int.Parse(chans[i]);
                    chanPositions[i] = i;
                }
                string labelString = el.Descendants("setname").FirstOrDefault().Value;
                groups.Add(new ECGroup { Chans = chanNums.ToList(), Label = labelString, Positions = chanPositions.ToList()});
            }

            // Parse the data (now that we know the size)
            Vector2[,] finalData = new Vector2[dataPointsPerChan, chanCount];
            byte[] shortBytes = new byte[2];
            for (int i = 0; i < dataPointsPerChan; i++)
            {
                for (int j = 0; j < chanCount; j++)
                {
                    int index = 64 + (i * totalChans + j) * 4;  
                    shortBytes[0] = rawData[index + 1]; // Big endian, unfortunately
                    shortBytes[1] = rawData[index];
                    float x = BitConverter.ToInt16(shortBytes, 0);
                    shortBytes[0] = rawData[index + 3];
                    shortBytes[1] = rawData[index + 2];
                    float y = BitConverter.ToInt16(shortBytes, 0);
                    finalData[i, j] = new Vector2(x, y);
                }
            }
            // Return a nice new ECData object!
            return new ECData(finalData, freqNames.ToArray(), groups, rots.ToArray(), measureScales.ToArray());
        }
    }
}
