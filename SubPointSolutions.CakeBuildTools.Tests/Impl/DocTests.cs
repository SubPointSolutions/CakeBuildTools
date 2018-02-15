using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubPointSolutions.CakeBuildTools.Services;
using SubPointSolutions.CakeBuildTools.Services.Indexes;
using SubPointSolutions.CakeBuildTools.Utils;

namespace SubPointSolutions.CakeBuildTools.Tests.Impl
{
    [TestClass]
    public class DocTests
    {


        [TestMethod]
        [TestCategory("CI.Core")]
        public void Can_CreateSamplesIndexes()
        {
            var expectedFolderNames = new Dictionary<string, bool>()
            {
                {"metapack", true },
                {"reSP", true },
                {"SPMeta2", true }
            };

            var assemblyPath = Path.GetDirectoryName(GetType().Assembly.Location);

            var path = @"C:\Users\anton\github\spmeta2\SPMeta2\SubPointSolutions.Docs";

            var rootPath = System.IO.Path.GetFullPath(path);
            var srcViewFolderPath = System.IO.Path.Combine(rootPath, "Views");

            Information(String.Format("Generating samples index in root folder:[{0}]", srcViewFolderPath));

            var srcSubmodulesPaths = System.IO.Directory.GetDirectories(srcViewFolderPath);

            Parallel.ForEach(srcSubmodulesPaths, srcFolderPath =>
            {
                var srcFolderName = new DirectoryInfo(srcFolderPath).Name;

                Information(String.Format("    Processing subfolder:[{0}]", srcFolderPath));

                SampleWriteAPI.CreateSamplesIndex<SampleIndexXmlService>(srcFolderPath);
                SampleWriteAPI.CreateSamplesIndex<SampleIndexJsonService>(srcFolderPath);

                var allSamples = SampleReadAPI.LoadSamples(srcFolderPath);

                var allSamplesDynamic = new List<dynamic>();
                foreach (var example in allSamples)
                {
                    dynamic d = DynamicUtils.ToDynamic(example);
                    allSamplesDynamic.Add(d);

                }

                Information(String.Format("        [{0}] - generated [{1}] sample index files", srcFolderName, allSamples.Count()));

                Assert.IsNotNull(allSamples);

                if (expectedFolderNames.ContainsKey(srcFolderName.ToLower()))
                {
                    Information(String.Format("        [{0}] - expecting more than 0 examples...", srcFolderName));
                    Assert.IsTrue(allSamples.Count() > 0);
                }
                else
                {
                    Information(String.Format("        [{0}] - Skipping example count validation...", srcFolderName));
                }
            });
        }


        #region utils

        protected void Information(string value)
        {
            Trace.WriteLine(value);
        }

        #endregion

    }
}
