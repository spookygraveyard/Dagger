using System;
using System.Collections.Generic;
using Windows.UI;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Dagger
{
    class ECDataFile
    {
        public string Index { get; set; }
        public string Row { get; set; }
        public string Col { get; set; }
        public string FullPath { get; set; }
        public string FileType { get; set; }
        public string Leg { get; set; }
        public Brush LegColour { get; set; }
        public int FileListIndex { get; set; }
        public string LoadedString { get; set; }
    }
}
