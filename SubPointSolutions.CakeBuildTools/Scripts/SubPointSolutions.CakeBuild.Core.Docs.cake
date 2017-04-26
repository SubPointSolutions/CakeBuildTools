#reference "tools/HtmlAgilityPack/lib/net45/HtmlAgilityPack.dll"

#reference "tools/System.Reflection.Metadata/lib/portable-net45+win8/System.Reflection.Metadata.dll"

#reference "tools/Microsoft.CodeAnalysis.Common/lib/net45/Microsoft.CodeAnalysis.dll"
#reference "tools/Microsoft.CodeAnalysis.CSharp/lib/net45/Microsoft.CodeAnalysis.CSharp.dll"

#reference "tools/SubPointSolutions.DocsBuildTools/lib/net45/SubPointSolutions.DocsBuildTools.dll"
#reference "tools/SubPointSolutions.DocsBuildTools.Data/lib/net45/SubPointSolutions.DocsBuildTools.Data.dll"

var defaultActionDocsGenerateSampleIndex = Task("Action-Docs-Generate-SampleIndex")
    .Does(() =>
{
			

            var rootPath = System.IO.Path.GetFullPath("../SubPointSolutions.Docs");
            var srcViewFolderPath = System.IO.Path.Combine(rootPath, "Views");

			Information(String.Format("Generating samples index in root folder:[{0}]", srcViewFolderPath));

            var srcSubmodulesPaths =  System.IO.Directory.GetDirectories(srcViewFolderPath);

            foreach (var srcFolderPath in srcSubmodulesPaths)
            {
				Information(String.Format("    Processing subfolder:[{0}]", srcFolderPath));
                SubPointSolutions.DocsBuildTools.SampleWriteAPI.CreateSamplesIndex(srcFolderPath);

				var allSamples = SubPointSolutions.DocsBuildTools.Data.SampleReadAPI.LoadSamples(srcFolderPath);
				Information(String.Format("        Generated [{0}] sample index files", allSamples.Count()));
            }
});