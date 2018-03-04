using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubPointSolutions.DocIndex.Services;
using SubPointSolutions.DocIndex.Services.Indexes;
using SubPointSolutions.DocIndex.Utils;

namespace SubPointSolutions.DocIndex.Tests.Impl
{
    [TestClass]
    public class DocIndexTests
    {
        #region constructors

        public DocIndexTests()
        {
            TestDataPath = Path.GetFullPath(@"../../Data");
        }

        #endregion

        #region properties

        public string TestDataPath { get; set; }

        #endregion

        [TestMethod]
        [TestCategory("CI.Core")]
        public void Can_CreateAndLoadSamples()
        {
            Log("Generating samples index in root folder: {0}", TestDataPath);

            var srcPaths = Directory.GetDirectories(TestDataPath);
            var dstPath = GetTmpFolder();

            Parallel.ForEach(srcPaths, srcFolderPath =>
            {
                var service = new DocIndexService();
                var srcFolderName = new DirectoryInfo(srcFolderPath).Name;

                var dstFolderPath = Path.Combine(dstPath, srcFolderName);
                Directory.CreateDirectory(dstFolderPath);

                Log("Pocessing content folder: {0} -> {1}", srcFolderPath, dstFolderPath);

                service.CreateSamplesIndex(srcFolderPath, dstFolderPath);

                var samples = service.LoadSamples(dstFolderPath).ToList();
                Log("[{0}] - generated [{1}] sample index files", srcFolderName, samples.Count());

                Assert.IsNotNull(samples);
                Assert.IsTrue(samples.Any());
            });
        }

        #region utils

        protected string GetTmpFolder()
        {
            var result = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(result);

            return result;
        }

        protected void Log(string message, params object[] args)
        {
            Log(string.Format(message, args));
        }

        protected void Log(string message)
        {
            Trace.WriteLine(message);
        }

        #endregion

    }
}
