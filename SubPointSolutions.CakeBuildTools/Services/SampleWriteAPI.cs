using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubPointSolutions.CakeBuildTools.Services.Indexes;

namespace SubPointSolutions.CakeBuildTools.Services
{
    public static class SampleWriteAPI
    {
        #region methods

        public static void CreateSamplesIndex<TSampleIndexService>(string srcFolderPath)
            where TSampleIndexService : SampleIndexServiceBase, new()
        {
            CreateSamplesIndex<TSampleIndexService>(srcFolderPath, null);
        }

        public static void CreateSamplesIndex<TSampleIndexService>(string srcFolderPath, string dstFodlerPath)
            where TSampleIndexService : SampleIndexServiceBase, new()
        {
            CreateSamplesIndex<TSampleIndexService>(srcFolderPath, dstFodlerPath, null);
        }

        public static void CreateSamplesIndex<TSampleIndexService>(string srcFolderPath, string dstFodlerPath, Action<SampleIndexSettings> setup)
            where TSampleIndexService : SampleIndexServiceBase, new()
        {
            var settings = new SampleIndexSettings
            {
                ContentSourceFolderPath = srcFolderPath,
                ContentDestinationFolderPath = dstFodlerPath
            };

            setup?.Invoke(settings);

            CreateSamplesIndex<TSampleIndexService>(settings);
        }

        public static void CreateSamplesIndex<TSampleIndexService>(SampleIndexSettings settings)
            where TSampleIndexService : SampleIndexServiceBase, new()
        {
            CreateSamplesIndex<TSampleIndexService>(settings, null);
        }

        public static void CreateSamplesIndex<TSampleIndexService>(SampleIndexSettings settings,
            Action<TSampleIndexService> config)
            where TSampleIndexService : SampleIndexServiceBase, new()
        {
            var service = new TSampleIndexService();

            if (config != null)
                config(service);

            service.GenerateSampleFiles(new SampleIndexSettings[]
            {
                settings
            });
        }

        #endregion
    }
}
