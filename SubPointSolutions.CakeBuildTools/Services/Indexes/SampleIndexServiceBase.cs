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
    public abstract class SampleIndexServiceBase
    {
        public abstract string GetSampleAsString(DocSample sample);

        public virtual string GetSampleFileName(DocSample sample)
        {
            return string.Format("{0}-{1}.{2}",
                sample.ClassName,
                sample.MethodName,
                "smp"
            );
        }

        public void GenerateSampleFiles(SampleIndexSettings settings)
        {
            GenerateSampleFiles(new SampleIndexSettings[]
            {
                settings
            });
        }

        public void GenerateSampleFiles(IEnumerable<SampleIndexSettings> sampleLookupSettings)
        {
            var assemblyPath = Path.GetDirectoryName(GetType().Assembly.Location);
            var samples = new List<SampleIndexSettings>();

            foreach (var sample in sampleLookupSettings)
            {
                CreateSamplesDbItem(sample.ContentSourceFolderPath,
                    sample.Resursive,
                    sample.ContentDestinationFolderPath);
            }
        }

        #region utils

        private void CreateSamplesDbItem(
            string srcFolderPath,
            bool recursive,
            string dstFolderPath)
        {
            if (!string.IsNullOrEmpty(dstFolderPath))
                Directory.CreateDirectory(dstFolderPath);

            var samples = GetAllSamples(srcFolderPath, recursive);

            foreach (var sample in samples)
            {
                var sampleDirectory = dstFolderPath ?? Path.Combine(sample.SourceFileFolder, "_samples");
                Directory.CreateDirectory(sampleDirectory);

                var className = sample.ClassName;
                var methodName = sample.MethodName;

                var fileName = GetSampleFileName(sample);
                var sampleFilePath = Path.Combine(sampleDirectory, fileName);

                var sampleAsText = GetSampleAsString(sample);

                File.WriteAllText(sampleFilePath, sampleAsText, Encoding.UTF8);
            }
        }

        private List<DocSample> GetAllSamples(string path, bool resursive)
        {
            var result = new List<DocSample>();

            var services = ReflectionUtils.GetTypesFromAssembly<SamplesServiceBase>(GetType().Assembly);

            foreach (var service in services.Select(a => Activator.CreateInstance(a) as SamplesServiceBase))
                result.AddRange(service.LoadSamples(path, resursive));

            return result;
        }

        #endregion
    }
}
