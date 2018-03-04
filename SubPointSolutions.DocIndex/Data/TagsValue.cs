using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SubPointSolutions.DocIndex.Data
{
    [DataContract]
    public class TagsValue
    {
        public TagsValue()
        {
            Values = new List<string>();
        }

        [DataMember]

        public string Name { get; set; }

        [DataMember]
        public List<string> Values { get; set; }

        [IgnoreDataMember]
        public string Value
        {
            get
            {
                return Values?.FirstOrDefault();
            }
            set
            {

            }
        }
    }
}
