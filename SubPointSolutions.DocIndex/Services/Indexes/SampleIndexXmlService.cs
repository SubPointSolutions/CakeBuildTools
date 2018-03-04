using SubPointSolutions.DocIndex.Data;

namespace SubPointSolutions.DocIndex.Services.Indexes
{
    public class SampleIndexXmlService : SampleIndexServiceBase
    {
        public static string FileExtension = "smpl.xml";

        public override string GetSampleFileName(DocSample sample)
        {
            return string.Format("{0}-{1}.{2}",
                sample.ClassName,
                sample.MethodName,
                FileExtension
            );
        }

        public override string GetSampleAsString(DocSample sample)
        {
            return sample.ToXml();
        }
    }
}
