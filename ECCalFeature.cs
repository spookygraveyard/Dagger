using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dagger
{
    class ECCalFeature
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
        public double RelLocation { get; set; }
        public int AbsPhase { get; set; }
        public double RelAmplitude { get; set; }
        public string ShortName { get; set; }
        public bool IsOptional { get; set; }
    }
}
