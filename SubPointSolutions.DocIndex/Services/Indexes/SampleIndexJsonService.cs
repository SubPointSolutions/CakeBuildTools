using SubPointSolutions.DocIndex.Data;
using SubPointSolutions.DocIndex.Utils;

namespace SubPointSolutions.DocIndex.Services.Indexes
{
    public class SampleIndexJsonService : SampleIndexServiceBase
    {
        public override string GetSampleFileName(DocSample sample)
        {
            return string.Format("{0}-{1}.{2}",
                sample.ClassName,
                sample.MethodName,
                "smpl-json"
            );
        }

        public override string GetSampleAsString(DocSample sample)
        {
            dynamic dynamicSample = sample.ToDynamic();
            return Newtonsoft.Json.JsonConvert.SerializeObject(dynamicSample);
        }
    }
}
