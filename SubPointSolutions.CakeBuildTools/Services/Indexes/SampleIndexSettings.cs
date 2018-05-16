using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubPointSolutions.CakeBuildTools.Services.Indexes
{
    public class SampleIndexSettings
    {
        public SampleIndexSettings()
        {
            Resursive = true;
        }

        public string ContentSourceFolderPath { get; set; }
        public string ContentDestinationFolderPath { get; set; }

        public bool Resursive { get; set; }
    }
}
