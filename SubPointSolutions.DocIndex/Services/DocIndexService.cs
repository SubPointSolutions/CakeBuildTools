using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubPointSolutions.DocIndex.Data;
using SubPointSolutions.DocIndex.Services.Processing;
using SubPointSolutions.DocIndex.Utils;
using SubPointSolutions.DocIndex.Services.Indexes;

namespace SubPointSolutions.DocIndex.Services
{
    public class DocIndexService
    {
        #region constructors

        public DocIndexService()
        {

        }

        #endregion

        #region properties

        #endregion

        #region write api

        public void CreateSamplesIndex(string srcPath)
        {
            CreateSamplesIndex(srcPath, (string)null);
        }

        public void CreateSamplesIndex(string srcPath, string dstPath)
        {
            CreateSamplesIndex(srcPath, setup =>
            {
                setup.ContentDestinationFolderPath = dstPath;
            });
        }

        public void CreateSamplesIndex(string path, Action<SampleIndexSettings> setup)
        {
            var service = new SampleIndexXmlService();
            var settings = new SampleIndexSettings
            {
                ContentSourceFolderPath = path,
                ContentDestinationFolderPath = null
            };

            setup?.Invoke(settings);

            service.GenerateSampleFiles(new SampleIndexSettings[]
            {
                settings
            });
        }

        public List<DocSample> LoadSamples(string path)
        {
            var result = new List<DocSample>();

            var allSampleFilePaths = Directory.GetFiles(path,
                                                        "*." + SampleIndexXmlService.FileExtension,
                                                        System.IO.SearchOption.AllDirectories);

            foreach (var allSampleFilePath in allSampleFilePaths)
            {
                var sampleXmlContent = File.ReadAllText(allSampleFilePath);
                var sampleInstance = DocSample.FromXml(sampleXmlContent);

                result.Add(sampleInstance);
            }

            return result;
        }

        #endregion

        #region utils

        #endregion
    }
}
