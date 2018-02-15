using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SubPointSolutions.CakeBuildTools.Data
{
    [DataContract]
    public class DocSampleTag
    {
        public DocSampleTag()
        {
            Values = new List<string>();
        }

        [DataMember]

        public string Name { get; set; }

        [DataMember]
        public List<string> Values { get; set; }
    }
}
