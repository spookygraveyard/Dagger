using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dagger
{
    public class ECRegion
    {
        public int start { get; set; }
        public int end { get; set; }
        public double amplitude { get; set; }
        public string label { get; set; }
        public int length { get; set; }
        public int channel { get; set; }
    }
}
