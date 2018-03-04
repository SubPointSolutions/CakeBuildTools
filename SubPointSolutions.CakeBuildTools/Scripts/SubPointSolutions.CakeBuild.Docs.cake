#tool nuget:https://www.myget.org/F/subpointsolutions-staging/api/v2?package=SubPointSolutions.DocIndex&prerelease&version=0.1.0-beta1

#reference "tools/SubPointSolutions.DocIndex.0.1.0-beta1/lib/net45/SubPointSolutions.DocIndex.dll"

#reference "tools/Microsoft.CodeAnalysis.Common.2.4.0/lib/netstandard1.3/Microsoft.CodeAnalysis.dll"
#reference "tools/Microsoft.CodeAnalysis.CSharp.2.4.0/lib/netstandard1.3/Microsoft.CodeAnalysis.CSharp.dll"
#reference "tools/HtmlAgilityPack.1.6.17/lib/net45/HtmlAgilityPack.dll"

var defaultActionDocsGenerateSampleIndex = Task("Action-Docs-Index")
    .Does(() =>
{
    if(jsonConfig["wyam"] == null) {
        Information("Skipping doc index build. 'wyam' section in config is empty");
    }

    var srcFolderPaths = jsonConfig["wyam"].SelectMany(c => c["inputDirs"]
                                        .Select( v => v.ToString()))
                                        .Distinct();
                                                

    Information("Building doc index for folders: {0}", srcFolderPaths.Count());
    var service = new SubPointSolutions.DocIndex.Services.DocIndexService();

    foreach (var srcFolderPath in srcFolderPaths)
    {
        var fullPath = ResolveFullPathFromSolutionRelativePath(srcFolderPath);

        Information(" - processing folder: {0}", fullPath);
        service.CreateSamplesIndex(fullPath);
    }

    Information("Building doc index completed!");
});