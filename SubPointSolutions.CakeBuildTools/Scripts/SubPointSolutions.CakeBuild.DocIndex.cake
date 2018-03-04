#tool nuget:https://www.nuget.org/api/v2/?package=Microsoft.CodeAnalysis.Common&version=2.4.0
#tool nuget:https://www.nuget.org/api/v2/?package=Microsoft.CodeAnalysis.CSharp&version=2.4.0

#tool nuget:https://www.nuget.org/api/v2/?package=HtmlAgilityPack&version=1.6.17
#tool nuget:https://www.nuget.org/api/v2/?package=SubPointSolutions.DocIndex

#reference "tools/Microsoft.CodeAnalysis.Common.2.4.0/lib/netstandard1.3/Microsoft.CodeAnalysis.dll"
#reference "tools/Microsoft.CodeAnalysis.CSharp.2.4.0/lib/netstandard1.3/Microsoft.CodeAnalysis.CSharp.dll"
#reference "tools/HtmlAgilityPack.1.6.17/lib/net45/HtmlAgilityPack.dll"

var defaultActionDocsGenerateSampleIndex = Task("Action-Docs-SampleIndex")
    .Does(() =>
{
	
});