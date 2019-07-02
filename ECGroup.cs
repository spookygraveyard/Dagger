using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dagger
{
    public class ECGroup
    {
        public List<int> Chans { get; set; }
        public List<int> Positions { get; set; }    // The channels might be out of order
        public string Label { get; set; }   // The name of the group
    }
}
