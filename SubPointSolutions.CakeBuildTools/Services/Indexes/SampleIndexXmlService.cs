using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubPointSolutions.CakeBuildTools.Data;
using SubPointSolutions.CakeBuildTools.Services.Processing;
using SubPointSolutions.CakeBuildTools.Utils;

namespace SubPointSolutions.CakeBuildTools.Services.Indexes
{
    public class SampleIndexXmlService : SampleIndexServiceBase
    {
        public override string GetSampleFileName(DocSample sample)
        {
            return string.Format("{0}-{1}.{2}",
                sample.ClassName,
                sample.MethodName,
                "smpl-xml"
            );
        }

        public override string GetSampleAsString(DocSample sample)
        {
            return sample.ToXml();
        }
    }
}
