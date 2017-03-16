// common tooling
// always version to avoid breaking change with new releases
#addin nuget:https://www.nuget.org/api/v2/?package=Cake.Powershell&Version=0.2.9
#addin nuget:https://www.nuget.org/api/v2/?package=newtonsoft.json&Version=9.0.1
#addin nuget:https://www.nuget.org/api/v2/?package=NuGet.Core&Version=2.12.0

#tool nuget:https://www.nuget.org/api/v2/?package=Octokit&Version=0.24.0
#tool nuget:https://www.nuget.org/api/v2/?package=RazorEngine&Version=3.8.2

#reference "tools/Octokit/lib/net45/Octokit.dll"
#reference "tools/Microsoft.Net.Http/lib/net40/System.Net.Http.dll"
#reference "tools/Microsoft.Net.Http/lib/net40/System.Net.Http.WebRequest.dll"
#reference "tools/Microsoft.AspNet.Razor/lib/net45/System.Web.Razor.dll"
#reference "tools/RazorEngine/lib/net40/RazorEngine.dll"

Information("Running SubPointSolutions.CakeBuildTools: 0.1.0-beta5");

// variables
// * defaultXXX - shared, common settings from json config
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

Verbose("Reading build.json");
var jsonConfig = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("build.json"));

// default helpers
var currentTimeStamp = String.Empty;

string GetGlobalEnvironmentVariable(string name) {
    var result = System.Environment.GetEnvironmentVariable(name, System.EnvironmentVariableTarget.Process);

    if(String.IsNullOrEmpty(result))
        result = System.Environment.GetEnvironmentVariable(name, System.EnvironmentVariableTarget.User);

    if(String.IsNullOrEmpty(result))
        result = System.Environment.GetEnvironmentVariable(name, System.EnvironmentVariableTarget.Machine);

    return result;
}

public class Release
    {
        public Release()
        {
            ReleaseIssueGroups = new List<ReleaseIssueGroup>();
        }

        public string ReleaseTitle { get; set; }
        public string ReleaseVersion { get; set; }
        public string ReleaseMonth { get; set; }

        public string ReleaseSummary { get; set; }

        public List<ReleaseIssueGroup> ReleaseIssueGroups { get; set; }


        public string RegressionTestNotes { get; set; }

        public string AssemblyFileVersion { get; set; }

        public ReleaseIssue TmpIssue { get; set; }
        public string ReleaseMonthAndYear { get; set; }

        public string ProjectName {get;set;}
        public string CompanyName {get;set;}
    }

public class ReleaseIssueGroup
    {
        public ReleaseIssueGroup()
        {
            Issues = new List<ReleaseIssue>();
        }

        public string Label { get; set; }
        public string LabelTitle { get; set; }

        public List<ReleaseIssue> Issues { get; set; }
    }

    public class ReleaseIssue
    {
        public string Title { get; set; }
        public int Number { get; set; }
        public string Url { get; set; }
    }

public class GiHubService {

 public GiHubService() {

     AppName = "SubPointSolutions.CakeBuildTools";
 }

    public string AppName { get; set; }

        public string UserName { get; set; }
        public string UserPassword { get; set; }

        protected Octokit.GitHubClient GClient { get; set; }

 public void EnsurePreRelease(
            string repositoryOwner, 
            string repositoryName,
            string releaseTag,
            string branchName,
            string releaseName,
            string releaseContent,
            bool publish)
        {
            var isDraft = !publish;

            WithG(client =>
            {
                var allReleases = client.Repository.Release.GetAll(repositoryOwner, repositoryName).Result;
                var existingRelease = allReleases.FirstOrDefault(r => r.Name == releaseName);

                if (existingRelease == null)
                {
                    var release = client.Repository.Release.Create(repositoryOwner, repositoryName, new Octokit.NewRelease(releaseTag)
                    {
                        Draft = isDraft,
                        Name = releaseName,
                        TargetCommitish = branchName,
                        Body = releaseContent
                    }).Result;
                }
                else
                {
                    var updatableRelease = existingRelease.ToUpdate();

                    updatableRelease.TagName = releaseTag;
                    updatableRelease.TargetCommitish = branchName;

                    updatableRelease.Body = releaseContent;
                    updatableRelease.Draft = isDraft;

                    var tmp = client.Repository.Release.Edit(repositoryOwner, repositoryName, existingRelease.Id, updatableRelease).Result;
                }
            });
        }

     protected virtual void InitGClient()
        {
            if (GClient == null)
                GClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue(AppName));

                var basicAuth = new Octokit.Credentials(UserName, UserPassword);
                GClient.Credentials = basicAuth;
        }

        protected virtual void WithG(Action<Octokit.GitHubClient> action)
        {
            InitGClient();

            action(GClient);
        }


    public List<Octokit.Issue> GetClosedIssuesInOpenedMilistones(string company, string repo)
        {
            return GetClosedIssuesInOpenedMilistones(company, repo, DateTimeOffset.Now.AddDays(8).DateTime);
        }

        public List<Octokit.Issue> GetClosedIssuesInOpenedMilistones(string company, string repo, DateTimeOffset nowDate)
        {
            var result = new List<Octokit.Issue>();

            WithG(client =>
            {
                var allMilestones = client.Issue.Milestone
                                      .GetAllForRepository(company, repo, new Octokit.MilestoneRequest
                                      {
                                          State = Octokit.ItemStateFilter.Open
                                      })
                                      .Result;

                var activeMilestones = allMilestones.Where(m => !m.ClosedAt.HasValue && m.DueOn <= nowDate)
                                                    .OrderBy(m => m.DueOn);

                var allClosedIssues = client.Issue.GetAllForRepository(company, repo, new Octokit.RepositoryIssueRequest
                {
                    State = Octokit.ItemStateFilter.Closed
                }).Result.ToList();

                foreach (var milestone in activeMilestones)
                {
                    foreach (var issue in allClosedIssues)
                    {
                        if (issue.Milestone != null &&
                            issue.Milestone.Title == milestone.Title)
                        {
                            if (issue.ClosedAt.HasValue)
                            {
                                result.Add(issue);
                            }
                        }
                    }
                }
            });

            return result;
        }

}

string CreateGitHubReleaseNotes(
        string githubCompanyName,
        string githubRepositoryName,

        string githubUserName,
        string githubUserPassword,

        string releaseTitle,
        string releaseVersion,

        string releaseAssemblyFileVersion,
        string releaseTemplateFileName,
        string releaseLabels
        
        ) {

            var useCache = false;
            var cacheFile = "release-cake.cache";

            var releaseData = new Release();

            releaseData.CompanyName = githubCompanyName;
            releaseData.ProjectName = releaseTitle;
            
            var githubAppName = "SubPointSolutions.CakeBuildTools";

            // var releaseLabels = string.Join(";", new string[]
            // {
            //     "new definition,New Definition",
            //     "bug,Fixes",
            //     "enhancement,Enhancements",
            //     "wontfix,"
            // });

            Information("Creating GitHub service...");

            var githubService = new GiHubService();

            githubService.AppName = githubAppName;

            githubService.UserName = githubUserName;
            githubService.UserPassword = githubUserPassword;

            if (System.IO.File.Exists(cacheFile) && useCache)
            {
                //releaseData = XmlSerializerUtils.DeserializeFromString<Release>(System.IO.File.ReadAllText(cacheFile));
            }
            else
            {
                var allowedIssueGroupLabels = releaseLabels.Split(';').Select(l => l.Split(',')[0]).ToList();
                var allowedIssueGroupLabelTitles = releaseLabels.Split(';').Select(l => l.Split(',')[1]).ToList();

                Information("Fetching closed issues...");

                var issues = new List<Octokit.Issue>();
                issues.AddRange(githubService.GetClosedIssuesInOpenedMilistones(githubCompanyName, githubRepositoryName));
                issues = issues.OrderByDescending(i => i.Number).ToList();

                foreach (var issue in issues)
                {
                    if (!issue.Labels.Any(l => allowedIssueGroupLabels.Contains(l.Name)))
                    {
                        throw new Exception(string.Format(
                            "Issue #{0} [{1}] [{2}] should have one of the following tags:[{3}]",
                            new object[] {
                            issue.Id, issue.Title, issue.Url,
                            string.Join(",", allowedIssueGroupLabels.ToArray()) }));
                    }
                }

                Information("Creating release groups...");
                var releaseIssueGroups = new List<ReleaseIssueGroup>();

                var index = 0;

                foreach (var allowedIssueLabel in allowedIssueGroupLabels)
                {
                    var issueGroupTitle = allowedIssueGroupLabelTitles[index++];

                    if (string.IsNullOrEmpty(issueGroupTitle))
                        continue;

                    releaseIssueGroups.Add(new ReleaseIssueGroup
                    {
                        Label = allowedIssueLabel,
                        LabelTitle = issueGroupTitle,
                        Issues = issues.Where(i => i.Labels.Any(l => l.Name == allowedIssueLabel))
                     .Select(i => new ReleaseIssue
                     {
                         Title = i.Title,
                         Url = string.Format("https://github.com/{0}/{1}/issues/{2}",
                                                githubCompanyName,
                                                githubRepositoryName,
                                                i.Number),
                         Number = i.Number
                     }).ToList()
                    });
                }

                releaseData.ReleaseIssueGroups = releaseIssueGroups;

                //var cache = XmlSerializerUtils.SerializeToString(releaseData);
                //System.IO.File.WriteAllText(cacheFile, cache);
            }

            Information("Filling release metadata...");
            releaseData.ReleaseMonthAndYear = string.Format("{0} {1}",
                                                DateTime.Now.ToString("MMMM"),
                                                DateTime.Now.Year);

            releaseData.ReleaseTitle = string.Format("{0}", releaseTitle);
            releaseData.ReleaseVersion = releaseVersion;
            releaseData.AssemblyFileVersion = releaseAssemblyFileVersion;

            Information("Fetching release notes template file:" + releaseTemplateFileName);
            var templateContent = System.IO.File.ReadAllText(releaseTemplateFileName);

            Information("Generating release notes...");

			var templateConfig = new RazorEngine.Configuration.TemplateServiceConfiguration();
			templateConfig.DisableTempFileLocking = true;
			templateConfig.CachingProvider = new RazorEngine.Templating.DefaultCachingProvider(t => {}); //disables the warnings

			var razorEngine = RazorEngine.Templating.RazorEngineService.Create(templateConfig);

            var result = RazorEngine.Templating.RazorEngineServiceExtensions.RunCompile(razorEngine, templateContent, "templateKey", null, releaseData);

            var releaseNotes = result;
            var fileName = string.Format("release-notes-{0}.md", releaseVersion);

            Information("Saving release notes to file: " + fileName);
            System.IO.File.WriteAllText(fileName, releaseNotes);

            

            var githubReleaseNotesBranch = ciBranch == "master" ? "master" : "beta";
            var githubReleaseNotesTitle = string.Format("{0} {1}, {2}", releaseTitle, releaseVersion, releaseData.ReleaseMonthAndYear);

            Information("Updating GitHub pre-release:" + githubReleaseNotesTitle);

            Verbose("   -githubCompanyName:" + githubCompanyName);
            Verbose("   -githubRepositoryName:" + githubRepositoryName);
            Verbose("   -releaseVersion:" + releaseVersion);
            Verbose("   -githubReleaseNotesBranch:" + githubReleaseNotesBranch);
            Verbose("   -releaseVersion:" + releaseVersion);

            if(ciGitHubShouldPublish) {
                Information("[!] Release will be published:" + githubReleaseNotesTitle);
            } else {
                Information("Release in draft:" + githubReleaseNotesTitle);
            }

            githubService.EnsurePreRelease(
                        githubCompanyName,
                        githubRepositoryName,
                        releaseVersion,
                        githubReleaseNotesBranch,
                        githubReleaseNotesTitle,
                        releaseNotes,
                        ciGitHubShouldPublish);

            return fileName;
}

string GetVersionForNuGetPackage(string id) {
    return GetVersionForNuGetPackage(id, ciBranch);
}

// get package id from json config
// dev - [json-id.-alpha+year+DayOfYear+Hour+Minute]
// beta - always from json config
string GetVersionForNuGetPackage(string id, string branch) {
    
    var resultVersion = string.Empty;
    var jsonPackageVersion = ResolveVersionForPackage(id); 

	if(branch != "master" && branch != "beta")
		branch = "dev";

    var now = DateTime.Now;

    Information(string.Format("Building nuget package version for branch:[{0}]", branch));

    switch(branch) {
       
        // dev always patches everything to alpha
        // - package.0.1.0-alpha170510045.nupkg
        // - package.0.1.0-alpha170510046.nupkg
        case "dev" : {

            var year = now.ToString("yy");
            var dayOfYear = now.DayOfYear.ToString("000");
            var timeOfDay = now.ToString("HHmm");

            var stamp = int.Parse(year + dayOfYear + timeOfDay);

            // save it to a static var to avoid flicks between NuGet package builds
            if(String.IsNullOrEmpty(currentTimeStamp))
                currentTimeStamp = stamp.ToString();

            var latestNuGetPackageVersion = jsonPackageVersion;
            Information(String.Format("- latest package [{0}] version [{1}]", id, latestNuGetPackageVersion));

            var versionParts = latestNuGetPackageVersion.Split('-');

            var packageVersion = String.Empty;
            var packageBetaVersion = 0;

            if(versionParts.Count() > 1) {
                packageVersion = versionParts[0];
                packageBetaVersion = int.Parse(versionParts[1].Replace("beta", String.Empty));
            } else {
                packageVersion = versionParts[0];
            }

            var currentVersion = new Version(packageVersion);

            Information(String.Format("- currentVersion package [{0}] version [{1}]", id, currentVersion));
            Information(String.Format("- packageBetaVersion package [{0}] version [{1}]", id, packageBetaVersion));

            var buildIncrement = 5;

            if(packageBetaVersion == 1) {
                resultVersion = string.Format("{0}.{1}.{2}",
                        new object[] {
                                currentVersion.Major,
                                currentVersion.Minor,
                                currentVersion.Build + buildIncrement
                }); 
            }

            var packageSemanticVersion = packageVersion + "-alpha" + (currentTimeStamp);
            resultVersion = packageSemanticVersion;

        }; break;

        // dev always gets the one from the jsonConfig
        // - package.0.1.0-beta1.nupkg
        // - package.0.1.0-beta2.nupkg
        case "beta" : {

            var latestNuGetPackageVersion = jsonPackageVersion;

            Information(String.Format("- latest package [{0}] version [{1}]", id, latestNuGetPackageVersion));
            resultVersion = latestNuGetPackageVersion;

            Information(String.Format("- currentVersion package [{0}] version [{1}]", id, resultVersion));
         }; break;

         // master always buils the major version removing '-beta' postfix
         // - package.0.1.0
         // - package.0.1.1 and so on
         case "master" : {

            var latestNuGetPackageVersion = jsonPackageVersion;

            Information(String.Format("- latest package [{0}] version [{1}]", id, latestNuGetPackageVersion));
            resultVersion = latestNuGetPackageVersion.Split('-')[0];

            Information(String.Format("- currentVersion package [{0}] version [{1}]", id, resultVersion));

         }; break;
    }

    return resultVersion;
}

string GetLatestPackageFromNuget(string packageId) {
    return GetLatestPackageFromNuget("https://packages.nuget.org/api/v2",packageId);
}

string GetLatestPackageFromNuget(string nugetRepoUrl, string packageId) {
    
    var repo =  NuGet.PackageRepositoryFactory.Default.CreateRepository(nugetRepoUrl);
    var package =  NuGet.PackageRepositoryExtensions.FindPackage(repo, packageId);

    if(package == null)
        return String.Empty;

    return package.Version.ToString();
}

string GetFullPath(string path) {
    return System.IO.Path.GetFullPath(path);
}

string ResolveFullPathFromSolutionRelativePath(string solutionRelativePath) {
    return System.IO.Path.GetFullPath(System.IO.Path.Combine(defaultSolutionDirectory, solutionRelativePath));
}

List<DirectoryPath> GetAllProjectDirectories(string solutionDirectory) {
    
    Verbose("Looking for *.csproj files in dir: " + solutionDirectory);

    var result = new List<DirectoryPath>();

    var csPrjFilePaths = System.IO.Directory.GetFiles(solutionDirectory, "*.csproj", System.IO.SearchOption.AllDirectories);

    foreach(var filePath in csPrjFilePaths)
    {
        var dirPath = System.IO.Path.GetDirectoryName(filePath);
        var binDirPath = System.IO.Path.Combine(dirPath, "bin");

        Verbose("- translated to bin path: " + binDirPath);
        result.Add(new DirectoryPath(binDirPath));
    }
    return result;
}

string ResolveVersionForPackage(string id) {

    Verbose(String.Format("Resolving deps for package id:[{0}]", id));

    var result = new List<NuSpecDependency>();
    var specs = jsonConfig["customNuspecs"];

    foreach(var spec in specs) {
        var specId = (string)spec["Id"];

        if(specId == id) {

            return (string)spec["Version"];
        }
    }

    // is it choco spec?
    specs = jsonConfig["customChocolateySpecs"];

	if(specs != null)
	{
		foreach(var spec in specs) {
			var specId = (string)spec["Id"];

			if(specId == id) {
				return (string)spec["Version"];
			}
		}
	}

    throw new Exception(String.Format("Cannot resolve version for package:[{0}]. Neither customNuspecs nor customChocolateySpecs has it", id));
}

bool IsLocalNuGetPackage(string id) {
    
    var specs = jsonConfig["customNuspecs"];

    foreach(var spec in specs) {

        if(id == (string)spec["Id"])
            return true;
    }

    return false;
}

ChocolateyPackSettings[] ResolveChocolateyPackSettings() {

    Verbose("Resolving Chocolatey specs..");
    var result = new List<ChocolateyPackSettings>();

    var specs = jsonConfig["customChocolateySpecs"];

    foreach(var spec in specs) {

        var packSettings = new ChocolateyPackSettings();

        packSettings.Id = (string)spec["Id"];
        packSettings.Version = (string)spec["Version"];

        

        if(spec["Authors"] == null)
            packSettings.Authors =  new [] { "SubPoint Solutions" };
        else
            packSettings.Authors = spec["Authors"].Select(t => (string)t).ToArray();

        if(spec["Owners"] == null)
            packSettings.Owners =  new [] { "SubPoint Solutions" };
        else
            packSettings.Owners = spec["Owners"].Select(t => (string)t).ToArray();
        
        packSettings.LicenseUrl = new System.Uri((string)spec["LicenseUrl"]);
        packSettings.ProjectUrl = new System.Uri((string)spec["ProjectUrl"]);
        packSettings.IconUrl = new System.Uri((string)spec["IconUrl"]);

        packSettings.Description = (string)spec["Description"];
        packSettings.Copyright = (string)spec["Copyright"];

        packSettings.Tags = spec["Tags"].Select(t => (string)t).ToArray();

        packSettings.RequireLicenseAcceptance = false;

        var files = spec["Files"].Select(t => t).ToArray();

        Verbose(String.Format("- resolving files [{0}]", files.Count())); 
        packSettings.Files = files.SelectMany(target => {
            
               Verbose(String.Format("Processing file set...")); 

               var result1 = new List<ChocolateyNuSpecContent>();
               
               var targetName = (string)target["Source"];
               Verbose(String.Format("- target name:[{0}]", targetName)); 

               var targetFilesFolder = (string)target["SolutionRelativeSourceFilesFolder"];
               Verbose(String.Format("- target files folder:[{0}]", targetFilesFolder)); 

               var targetFiles = target["SourceFiles"].Select(t => (string)t).ToArray();
               Verbose(String.Format("- target files:[{0}]", targetFiles.Count())); 

               var absFileFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(defaultSolutionDirectory, targetFilesFolder));

                foreach(var f in targetFiles)
                {
                    Verbose(String.Format("     - resolving file:[{0}]", f)); 

                    if(f.Contains("**")) {

                        var dstFolder = f.Replace("**", String.Empty).TrimEnd('\\').Replace('\\', '/');
                        var srcFolder = System.IO.Path.GetFullPath(
                                                System.IO.Path.Combine(defaultSolutionDirectory,
                                                    System.IO.Path.Combine(targetFilesFolder, dstFolder)));

                        //if(!System.IO.Directory.Exists(srcFolder))
                        //    throw new Exception(String.Format("Directory does not exist: [{0}]"));

                        var chAbsSrcDir = srcFolder +  @"\**";
                        var chDstDir = targetName + "/" + dstFolder.TrimEnd('/');

                        Verbose(String.Format("     - resolved as:[{0}]", chAbsSrcDir)); 

                        result1.Add( new ChocolateyNuSpecContent{
                            Source = chAbsSrcDir,
                            Target = chDstDir
                        });
                    }
                    else{
                        
                        
                        var singleFileAbsolutePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(absFileFolder, f));
                        
                        Verbose(String.Format("     - resolved as:[{0}]", singleFileAbsolutePath)); 

                        //if(!System.IO.File.Exists(singleFileAbsolutePath))
                        //    throw new Exception(String.Format("File does not exist: [{0}]", singleFileAbsolutePath));

                        result1.Add( new ChocolateyNuSpecContent{
                            Source = singleFileAbsolutePath,
                            Target = targetName
                        });
                    }
                }

                return result1;

            }).ToList();

        packSettings.OutputDirectory = new DirectoryPath(defaultChocolateyPackagesDirectory);

        // patch package versions 
        packSettings.Version = GetVersionForNuGetPackage(packSettings.Id); 

        result.Add(packSettings);
    }

    return result.ToArray();
}

NuGetPackSettings[] ResolveNuGetPackSettings() {

    var result = new List<NuGetPackSettings>();

    var specs = jsonConfig["customNuspecs"];

    foreach(var spec in specs) {

        var packSettings = new NuGetPackSettings();

        packSettings.Id = (string)spec["Id"];
        packSettings.Version = (string)spec["Version"];

        packSettings.Dependencies = ResolveDependenciesForPackage(packSettings.Id);

		Verbose("Adding Authors/Owners...");
        if(spec["Authors"] == null)
            packSettings.Authors =  new [] { "SubPoint Solutions" };
        else
            packSettings.Authors = spec["Authors"].Select(t => (string)t).ToArray();

        if(spec["Owners"] == null)
            packSettings.Owners =  new [] { "SubPoint Solutions" };
        else
            packSettings.Owners = spec["Owners"].Select(t => (string)t).ToArray();
        
		Verbose("Adding License/ProjectUrl/IconUrl...");
        packSettings.LicenseUrl = new System.Uri((string)spec["LicenseUrl"]);
        packSettings.ProjectUrl = new System.Uri((string)spec["ProjectUrl"]);
        packSettings.IconUrl = new System.Uri((string)spec["IconUrl"]);

		Verbose("Adding Description/Copyright...");
        packSettings.Description = (string)spec["Description"];
        packSettings.Copyright = (string)spec["Copyright"];

		Verbose("Adding tags...");
        packSettings.Tags = spec["Tags"].Select(t => (string)t).ToArray();

        packSettings.RequireLicenseAcceptance = false;
        packSettings.Symbols = false;
        packSettings.NoPackageAnalysis = false;

        // files
        var packageFilesObject =  spec["Files"];

		Verbose("Adding files...");

        // default files
        if(packageFilesObject == null || packageFilesObject.Select(t => t).Count() == 0)
        {
			var packageFiles = packageFilesObject;

            Verbose("Adding default files - *.dll/*.xml from bin/debug");
			
            var projectPath = System.IO.Path.Combine(defaultSolutionDirectory, packSettings.Id);

			var customProjectFolder = (string)spec["CustomProjectFolder"];
			if(!String.IsNullOrEmpty(customProjectFolder))
				projectPath = System.IO.Path.Combine(defaultSolutionDirectory, customProjectFolder);

            var projectBinPath = System.IO.Path.Combine(projectPath, "bin/debug");

            packSettings.BasePath = projectBinPath;

            packSettings.Files = new [] {
                    new NuSpecContent {
                        Source = packSettings.Id + ".dll",
                        Target = "lib/net45"
                    },
                    new NuSpecContent {
                        Source = packSettings.Id + ".xml",
                        Target = "lib/net45"
                    }
            };
        }
        else{
			
			var packageFiles = packageFilesObject;

            Verbose("Adding custom files...");

            var nuSpecContentFiles = new List<NuSpecContent>();
          
		    var projectPath = System.IO.Path.Combine(defaultSolutionDirectory, packSettings.Id);

            var customProjectFolder = (string)spec["CustomProjectFolder"];

			if(!String.IsNullOrEmpty(customProjectFolder))
				projectPath = System.IO.Path.Combine(defaultSolutionDirectory, customProjectFolder);

			Verbose("Project path: " + projectPath);

            var projectBinPath = System.IO.Path.Combine(projectPath, "bin/debug");

            packSettings.BasePath = projectPath;

            foreach(var packageFile in packageFiles){

                var target = (string)packageFile["Target"];

                foreach(var srcFile in packageFile["TargetFiles"].Select(t => (string)t).ToArray())
                {
                    nuSpecContentFiles.Add( new NuSpecContent {
                                Source = srcFile,
                                Target = target
                            });
                }                
            }

            packSettings.Files = nuSpecContentFiles.ToArray();
        }

        packSettings.OutputDirectory = new DirectoryPath(defaultNuGetPackagesDirectory);

        // patch package versions for dev build
        packSettings.Version = GetVersionForNuGetPackage(packSettings.Id); 

       foreach(var dep in packSettings.Dependencies){
           if(IsLocalNuGetPackage(dep.Id))
               dep.Version = GetVersionForNuGetPackage(packSettings.Id); 
        }

        result.Add(packSettings);
    }

    return result.ToArray();
}

NuSpecDependency[] ResolveDependenciesForPackage(string id) {

    Verbose(String.Format("Resolving deps for package id:[{0}]", id));

    var result = new List<NuSpecDependency>();
    var specs = jsonConfig["customNuspecs"];

    foreach(var spec in specs) {
        var specId = (string)spec["Id"];

        if(specId == id) {

            var deps = spec["Dependencies"];

            foreach(var dep in deps) {
                result.Add(new NuSpecDependency() {
                    Id = (string)dep["Id"],
                    Version = (string)dep["Version"],
                });
            }

            break;
        }
    }

    foreach(var dep in result) {
        Information(String.Format(" - ID:[{0}] Version:[{1}]", dep.Id, dep.Version));
    }

    return result.ToArray();
}

// CI related environment
// * dev / beta / master versioning and publishing
var ciBranch = GetGlobalEnvironmentVariable("ci.activebranch") ?? "local";

// override under CI run
var ciBranchOverride = GetGlobalEnvironmentVariable("APPVEYOR_REPO_BRANCH");
if(!String.IsNullOrEmpty(ciBranchOverride))
{
    Information(String.Format("Detected APPVEYOR build. Reverting to APPVEYOR_REPO_BRANCH varibale:[{0}]", ciBranchOverride));
	ciBranch = ciBranchOverride;
}

var ciNuGetSource = GetGlobalEnvironmentVariable("ci.nuget.source") ?? String.Empty;
var ciNuGetKey = GetGlobalEnvironmentVariable("ci.nuget.key") ?? String.Empty;
var ciNuGetShouldPublish = bool.Parse(GetGlobalEnvironmentVariable("ci.nuget.shouldpublish") ?? "FALSE");

var ciGitHubShouldPublish = bool.Parse(GetGlobalEnvironmentVariable("ci.github.shouldpublish") ?? "FALSE");

// source solution dir and file
var defaultSolutionDirectory =  GetFullPath((string)jsonConfig["defaultSolutionDirectory"]); 
var defaultSolutionFilePath = GetFullPath((string)jsonConfig["defaultSolutionFilePath"]);

// nuget packages
var defaultNuGetPackagesDirectory = GetFullPath((string)jsonConfig["defaultNuGetPackagesDirectory"]);
System.IO.Directory.CreateDirectory(defaultNuGetPackagesDirectory);

var defaultNuspecVersion = (string)jsonConfig["defaultNuspecVersion"];

// chocolatey
var defaultChocolateyPackagesDirectory = GetFullPath((string)jsonConfig["defaultChocolateyPackagesDirectory"]);
System.IO.Directory.CreateDirectory(defaultChocolateyPackagesDirectory);

// test settings
var defaultTestCategories = jsonConfig["defaultTestCategories"].Select(t => (string)t).ToList();
var defaultTestAssemblyPaths = jsonConfig["defaultTestAssemblyPaths"].Select(t => GetFullPath(defaultSolutionDirectory + "/" + (string)t)).ToList();

// build settings
var defaultBuildDirs = jsonConfig["defaultBuildDirs"].Select(t => new DirectoryPath(GetFullPath((string)t))).ToList();
var defaultEnvironmentVariables =  jsonConfig["defaultEnvironmentVariables"].Select(t => (string)t).ToList();

// refine defaultBuildDirs - everything with *.csprj in the folder + /bin
//effectively, looking for all cs projects within solution
defaultBuildDirs.AddRange(GetAllProjectDirectories(defaultSolutionDirectory));

// default dirs for chocol and nuget packages
defaultBuildDirs.Add(ResolveFullPathFromSolutionRelativePath(defaultChocolateyPackagesDirectory));
defaultBuildDirs.Add(ResolveFullPathFromSolutionRelativePath(defaultNuGetPackagesDirectory));

Information("Starting build...");
Information(string.Format(" -target:[{0}]",target));
Information(string.Format(" -configuration:[{0}]", configuration));
Information(string.Format(" -activeBranch:[{0}]", ciBranch));

var defaultNuspecs = new List<NuGetPackSettings>();
defaultNuspecs.AddRange(ResolveNuGetPackSettings());

var defaulChocolateySpecs = new List<ChocolateyPackSettings>();
defaulChocolateySpecs.AddRange(ResolveChocolateyPackSettings());

// validates that all defaultEnvironmentVariables exist
var defaultActionValidateEnvironment = Task("Action-Validate-Environment")
    .Does(() =>
{
    foreach(var name in defaultEnvironmentVariables)
    {
        Information(string.Format("HasEnvironmentVariable - [{0}]", name));
        if(!HasEnvironmentVariable(name)) {
            Information(string.Format("Cannot find environment variable:[{0}]", name));
            throw new ArgumentException(string.Format("Cannot find environment variable:[{0}]", name));
        }
    }
});

// cleans everything from defaultBuildDirs
var defaultActionClean = Task("Action-Clean")
    .Does(() =>
{
    foreach(var dirPath in defaultBuildDirs) {
        CleanDirectory(dirPath);
    }        
});


// restores NuGet packages for default solution
var defaultActionRestoreNuGetPackages = Task("Action-Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore(defaultSolutionFilePath);
});

// buulds current solution
var defaultActionBuild = Task("Action-Build")
    .Does(() =>
{
      MSBuild(defaultSolutionFilePath, settings => {
            settings.SetVerbosity(Verbosity.Quiet);
            settings.SetConfiguration(configuration);
      });
});

// runs unit tests for the giving projects and categories
var defaultActionRunUnitTests = Task("Action-Run-UnitTests")
    .Does(() =>
{
    foreach(var assemblyPath in defaultTestAssemblyPaths) {
        
        foreach(var testCategory in defaultTestCategories) {
            Information(string.Format("Running test category [{0}] for assembly:[{1}]", testCategory, assemblyPath));

            MSTest(new [] { new FilePath(assemblyPath) }, new MSTestSettings {
                    Category = testCategory
                });
        }
    }        
});

// creates NuGet packages for default NuSpecs
var defaultActionAPINuGetPackaging =Task("Action-API-NuGet-Packaging")
    .Does(() =>
{
    Information("Creating NuGet packages in directory:[{0}]", new []{
        defaultNuGetPackagesDirectory,
        defaultNuspecVersion
    });

    CreateDirectory(defaultNuGetPackagesDirectory);
    CleanDirectory(defaultNuGetPackagesDirectory);

    var currentIndex = 1;
    var totalCount = defaultNuspecs.Count;

    foreach(var nuspec in defaultNuspecs)
    {   
        Information(string.Format("[{2}/{3}] - Creating NuGet package for [{0}] of version:[{1}]", 
        new object[] {
                nuspec.Id, 
                nuspec.Version,
                currentIndex,
                currentIndex
        }));

        NuGetPack(nuspec);
        currentIndex++;
    }        
});

bool ShouldPublishAPINuGet(string branch) {

    // always publish dev branch
    // the rest comes from 'ciNuGetShouldPublish' -> 'ci.nuget.shouldpublish' environment variable
	if(branch == "dev")
		return true;

	return ciNuGetShouldPublish;
}

var defaultActionAPINuGetPublishing = Task("Action-API-NuGet-Publishing")
    // all packaged should be compiled by NuGet-Packaging task into 'defaultNuGetPackagesDirectory' folder
    .Does(() =>
{
    Information(String.Format("API NuGet publishing enabled? branch:[{0}]", ciBranch));
	var shouldPublish = ShouldPublishAPINuGet(ciBranch);

    var nugetSource = String.Empty;
	var nugetKey = String.Empty;

    if(!shouldPublish) {
        Information("Skipping NuGet publishing.");
        return;
    } else {
        Information("Fetching NuGet feed creds.");

        var feedSourceVariableName = String.Format("ci.nuget.{0}-source", ciBranch);
        var feedKeyVariableName = String.Format("ci.nuget.{0}-key", ciBranch);

        var feedSourceValue = GetGlobalEnvironmentVariable(feedSourceVariableName);
        var feedKeyValue = GetGlobalEnvironmentVariable(feedKeyVariableName);

        if(String.IsNullOrEmpty(feedSourceValue)) 
            throw new Exception(String.Format("environment variable is null or empty:[{0}]", feedSourceVariableName));

        if(String.IsNullOrEmpty(feedKeyValue)) 
            throw new Exception(String.Format("environment variable is null or empty:[{0}]", feedKeyVariableName));

        nugetSource = feedSourceValue;
        nugetKey = feedKeyValue;
    }

    Information("Publishing NuGet packages to repository: [{0}]", new []{
        nugetSource
    });

    var nuGetPackages = System.IO.Directory.GetFiles(defaultNuGetPackagesDirectory, "*.nupkg");

    foreach(var packageFilePath in nuGetPackages)
        {
            var packageFileName = System.IO.Path.GetFileName(packageFilePath);

            if(System.IO.File.Exists(packageFilePath)) {
                
                // checking is publushed
                Information(string.Format("Checking if NuGet package [{0}] is already published", packageFileName));
                
                // TODO
                var isNuGetPackagePublished = false;
                if(!isNuGetPackagePublished)
                {
                    Information(string.Format("Publishing NuGet package [{0}]...", packageFileName));
                
                    NuGetPush(packageFilePath, new NuGetPushSettings {
                        Source = nugetSource,
                        ApiKey = nugetKey
                    });
                }
                else
                {
                    Information(string.Format("NuGet package [{0}] was already published", packageFileName));
                }                 
                
            } else {
                Information(string.Format("NuGet package does not exist:[{0}]", packageFilePath));
                throw new ArgumentException(string.Format("NuGet package does not exist:[{0}]", packageFilePath));
            }
        }           
});

var defaultActionCLIChocolateyPackaging = Task("Action-CLI-Chocolatey-Packaging")
    .Does(() =>
{
      Information("Building CLI - Chocolatey package...");

      foreach(var chocoSpec in defaulChocolateySpecs) {

           chocoSpec.Version = GetVersionForNuGetPackage(chocoSpec.Id);

           Information(string.Format("Creating Chocolatey package [{0}] version:[{1}]", chocoSpec.Id, chocoSpec.Version));
           ChocolateyPack(chocoSpec);
       }

      Information(string.Format("Completed creating chocolatey package"));
});

var defaultActionCLIZipPackaging = Task("Action-CLI-Zip-Packaging")
    .Does(() =>
{
      Information("Building CLI - Zip package...");

      foreach(var chocoSpec in defaulChocolateySpecs) {

           var cliId = chocoSpec.Id;
           var cliVersion = chocoSpec.Version;

           Information("Building Zip package for chocolatey spec:[{0}] version:[{1}]", cliId, cliVersion);

           var tmpFolderPath = System.IO.Path.Combine( System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
           System.IO.Directory.CreateDirectory(tmpFolderPath);
           
           var files = new List<string>();

           foreach(var contentSpec in chocoSpec.Files)
           {
               var f = contentSpec.Source;

               if(f.Contains("**")) {

                    var dstFolder = f.Replace("**", String.Empty).TrimEnd('\\').Replace('\\', '/');
                    files.AddRange(System.IO.Directory.GetFiles(dstFolder, "*.*").Select(fl => System.IO.Path.GetFullPath(fl)));
               }
               else{
                   files.Add(f);
               }
           }
           
           var originalFileBase = System.IO.Path.GetDirectoryName(files.FirstOrDefault(f => f.Contains(".exe")));

           Verbose(String.Format("- base folder:[{0}]",originalFileBase));
           foreach(var f in files)
           Verbose(String.Format(" - file:[{0}]", f));
           
           Information(string.Format("Copying distr files to TMP folder [{0}]", tmpFolderPath));

            foreach(var filePath in files) {

                Information(string.Format("src file:[{0}]", filePath));    
                
                    var absPath = filePath
                                    .Replace(originalFileBase, "")
                                    .Replace(System.IO.Path.GetFileName(filePath), "")
                                    .Trim('\\')
                                    .Trim('/');
                    
                    var dstDirectory = System.IO.Path.Combine(tmpFolderPath, absPath);
                    var dstFilePath = System.IO.Path.Combine(dstDirectory,  System.IO.Path.GetFileName(filePath));

                    System.IO.Directory.CreateDirectory(dstDirectory);

                    Information("- copy from: " + filePath);
                    Information("- copy to  : " + dstFilePath);
                    System.IO.File.Copy(filePath, dstFilePath);
                }

                //packaging
                  var cliZipPackageFileName =  String.Format("{0}.{1}.zip", cliId, cliVersion);
                  var cliZipPackageFilePath = System.IO.Path.Combine(defaultChocolateyPackagesDirectory, cliZipPackageFileName);

                  Information(string.Format("Creating ZIP file [{0}]", cliZipPackageFileName));
                  Zip(tmpFolderPath, cliZipPackageFilePath);

                  Information(string.Format("Calculating checksum..."));

                  var md5 = CalculateFileHash(cliZipPackageFilePath, HashAlgorithm.MD5);
                  var sha256 = CalculateFileHash(cliZipPackageFilePath, HashAlgorithm.SHA256);
                  var sha512 = CalculateFileHash(cliZipPackageFilePath, HashAlgorithm.SHA512);

                  Information(string.Format("-md5    :{0}", md5.ToHex()));
                  Information(string.Format("-sha256 :{0}", sha256.ToHex()));
                  Information(string.Format("-sha512 :{0}", sha512.ToHex()));

                  var md5FileName = cliZipPackageFileName + ".MD5SUM";
                  var sha256FileName = cliZipPackageFileName  + ".SHA256SUM";
                  var sha512FileName = cliZipPackageFileName  + ".SHA512SUM";

                  var md5FilePath = cliZipPackageFilePath + ".MD5SUM";
                  var sha256FilePath = cliZipPackageFilePath  + ".SHA256SUM";
                  var sha512FilePath = cliZipPackageFilePath  + ".SHA512SUM";

                  System.IO.File.WriteAllLines(md5FilePath, new string[]{
                      String.Format("{0} {1}", md5.ToHex(), md5FileName)
                  });

                  System.IO.File.WriteAllLines(sha256FilePath, new string[]{
                      String.Format("{0} {1}", sha256.ToHex(), sha256FileName)
                  });

                  System.IO.File.WriteAllLines(sha512FilePath, new string[]{
                      String.Format("{0} {1}", sha512.ToHex(), sha512FileName)
                  });

                  var distrFiles = new string[] {
                      cliZipPackageFilePath,
                      md5FilePath,
                      sha256FilePath,
                      sha512FilePath
                  };

                  Information("Final distributive:");
                  foreach(var filePath in distrFiles)
                  {
                      Information(string.Format("- {0}", filePath));
                  }

      }

    
});

var defaultActionDocsMerge = Task("Action-Docs-Merge")
    .Does(() =>
{
    Information("Building documentation merge...");

	if(jsonConfig["defaultDocsBuildEnabled"] == null || ((bool)jsonConfig["defaultDocsBuildEnabled"]) == false) {
		 Information(String.Format("defaultDocsBuildEnabled is null. No docs merge will be done."));
 		 return;
	}

    var defaultDocsViewFolder = (string)jsonConfig["defaultDocsViewFolder"];
    if(String.IsNullOrEmpty(defaultDocsViewFolder))
        throw new Exception("defaultDocsViewFolder is null or empty. Update json config");

    var defaultDocsRepoFolder = (string)jsonConfig["defaultDocsRepoFolder"];
    if(String.IsNullOrEmpty(defaultDocsRepoFolder))
        throw new Exception("defaultDocsRepoFolder is null or empty. Update json config");

    var srcDocsPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(defaultSolutionDirectory,defaultDocsViewFolder ));
    Information(String.Format("Merging from folder [{0}]", srcDocsPath));

    if(!System.IO.Directory.Exists(srcDocsPath))
        throw new Exception(String.Format("Directory does not exist - [{0}]", srcDocsPath));

    var defaultDocsBranch = (string)jsonConfig["defaultDocsBranch"];
    if(String.IsNullOrEmpty(defaultDocsBranch))
        throw new Exception("defaultDocsBranch is null or empty. Update json config");

    var defaultDocsFileExtensions = jsonConfig["defaultDocsFileExtensions"]
                                        .Select(f => (string)f)
                                        .ToList().OrderBy(a => a);

    if(defaultDocsFileExtensions.Count() == 0)
        throw new Exception("defaultDocsFileExtensions is null or empty. Update json config with array of extensions to be added to docs");

        // because of some long names files - always in the root
     var tmpDocsFolder = System.IO.Path.Combine("c:/","__sps_docs");

    var docsRepoFolder = System.IO.Path.GetFullPath(string.Format(@"{0}/{1}",  tmpDocsFolder, defaultDocsRepoFolder));
    var docsRepoUrl = @"https://github.com/SubPointSolutions/subpointsolutions-docs";

    var dstDocsPath = string.Format(@"{0}/subpointsolutions-docs/SubPointSolutions.Docs/Views", docsRepoFolder);
	var dstDocsSamplesPath = string.Format(@"{0}/subpointsolutions-docs/SubPointSolutions.Docs", docsRepoFolder);

    var docsEnvironmentVars = new [] {
        "ci.docs.username",
        "ci.docs.userpassword",
    };

    foreach(var name in docsEnvironmentVars)
    {
        Information(string.Format("HasEnvironmentVariable - [{0}]", name));

        var value = GetGlobalEnvironmentVariable(name);
        
        if(String.IsNullOrEmpty(value)) {
            Information(string.Format("Cannot find environment variable:[{0}]", name));
            throw new ArgumentException(string.Format("Cannot find environment variable:[{0}]", name));
        }
    }

     var docsRepoUserName = GetGlobalEnvironmentVariable("ci.docs.username");
	 var docsRepoUserPassword = GetGlobalEnvironmentVariable("ci.docs.userpassword");
     var docsRepoPushUrl = string.Format(@"https://{0}:{1}@github.com/SubPointSolutions/subpointsolutions-docs", docsRepoUserName, docsRepoUserPassword);

     var commitName = string.Format(@"MetaPack - CI docs merge {0}", DateTime.Now.ToString("yyyyMMdd_HHmmssfff"));

     Information(string.Format("Merging documentation with commit:[{0}]", commitName));

     Information(string.Format("Cloning repo [{0}] with branch [{2}] to [{1}]", 
                docsRepoUrl, docsRepoFolder, defaultDocsBranch));

     if(!System.IO.Directory.Exists(docsRepoFolder))
     {   
        System.IO.Directory.CreateDirectory(docsRepoFolder);   

        var cloneCmd = new []{
            string.Format("cd '{0}'", docsRepoFolder),
            string.Format("git clone -b {1} {0}", docsRepoUrl, defaultDocsBranch)
        };

        StartPowershellScript(string.Join(Environment.NewLine, cloneCmd));  
     }                            

     docsRepoFolder = docsRepoFolder + "/subpointsolutions-docs"; 

     Information(string.Format("Checkout docs branch:[{0}]", defaultDocsBranch));
     var checkoutCmd = new []{
            string.Format("cd '{0}'", docsRepoFolder),
            string.Format("git checkout {0}", defaultDocsBranch),
            string.Format("git pull")
      };

      StartPowershellScript(string.Join(Environment.NewLine, checkoutCmd));  

      // there must be *.sln file after check out
      // if not - then it means we failed to connect - check out
      var hasSolutionFile =  System.IO.Directory.GetFiles(docsRepoFolder, "*.sln", System.IO.SearchOption.AllDirectories).Count() != 0;
      if(!hasSolutionFile) {
          
		  Information(string.Format("Cannot find *.sln file in folder:[{0}]", docsRepoFolder));
		  Information(string.Format("Cleaning folder...", docsRepoFolder));
		  // clean up and try again
           System.IO.Directory.Delete(docsRepoFolder, true);

          // again..
         Information(string.Format("Checkout docs branch:[{0}]", defaultDocsBranch));
         StartPowershellScript(string.Join(Environment.NewLine, checkoutCmd));  

        hasSolutionFile =  System.IO.Directory.GetFiles(docsRepoFolder, "*.sln", System.IO.SearchOption.AllDirectories).Count() != 0;

        if(!hasSolutionFile) {
            throw new Exception(String.Format("Cannot check out docs repo. Can't find solution file in folder:[{0}]", docsRepoFolder));
        }
      }      

      Information(string.Format("Merge and commit..."));
      var mergeCmd = new List<String>();
      
      mergeCmd.Add(string.Format("cd '{0}'", docsRepoFolder));
      mergeCmd.Add(string.Format("copy-item  '{0}' '{1}' -Recurse -Force", srcDocsPath,  dstDocsPath));

	  // copying samples
	  var defaultDocsSampleFilesFolder = (string)jsonConfig["defaultDocsSampleFilesFolder"];

	  if(!String.IsNullOrEmpty(defaultDocsSampleFilesFolder))
	  {
		var defaultDocsSampleFilesFolderFullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(defaultSolutionDirectory,defaultDocsSampleFilesFolder));
		mergeCmd.Add(string.Format("copy-item  '{0}' '{1}' -Recurse -Force", defaultDocsSampleFilesFolderFullPath,  dstDocsSamplesPath));
	  }

      foreach(var defaultDocsFileExtension in defaultDocsFileExtensions) {
        Verbose(String.Format(" - adding extension:[{0}]",defaultDocsFileExtension));
        mergeCmd.Add(string.Format("git add {0} -f", defaultDocsFileExtension));
      }

      mergeCmd.Add(string.Format("git commit -m '{0}'", commitName));

      StartPowershellScript(string.Join(Environment.NewLine, mergeCmd)); 

      Information(string.Format("Push to the main repo..."));
      var pushCmd = new []{
            string.Format("cd '{0}'", docsRepoFolder),
            string.Format("git config http.sslVerify false"),
			string.Format("git config --global push.default simple"),
            string.Format("git push {0}", docsRepoPushUrl)
      };

	  // writing a temporary PS file to avoid creds exposure in the build output
      var pushCmdFilePath = System.IO.Path.GetTempFileName() + ".ps1";
      System.IO.File.WriteAllLines(pushCmdFilePath, pushCmd);

	  //   var res = StartPowershellScript(string.Join(Environment.NewLine, pushCmd), new PowershellSettings()
	  //   {
	  //         LogOutput = false,
	  //         OutputToAppConsole  = false
	  //   });

      var res = StartPowershellFile(pushCmdFilePath, new PowershellSettings()
      {
            LogOutput = false,
            OutputToAppConsole  = false
      });

      Information(string.Format("Completed docs merge.")); 
});

var defaultActionGitHubReleaseNotes = Task("Action-GitHub-ReleaseNotes")
    .Does(() => {

    Information("Building GitHub release notes...");

    string githubCompanyName = GetGlobalEnvironmentVariable("ci.github.companyname");
    string githubRepositoryName = GetGlobalEnvironmentVariable("ci.github.repositoryname");
    string githubUserName = GetGlobalEnvironmentVariable("ci.github.username");
    string githubUserPassword = GetGlobalEnvironmentVariable("ci.github.userpassword");
    
    string releaseTitle = GetGlobalEnvironmentVariable("ci.github.releasetitle");
    string releaseVersion = GetGlobalEnvironmentVariable("ci.github.releaseversion");
    string releaseAssemblyFileVersion = GetGlobalEnvironmentVariable("ci.github.releaseAssemblyFileVersion");
    
    string releaseTemplateFileName = GetGlobalEnvironmentVariable("ci.github.releaseTemplateFileName");
    
    if(string.IsNullOrEmpty(releaseTemplateFileName))
        releaseTemplateFileName = "github-release-notes.chtml";

    string releaseLabels = GetGlobalEnvironmentVariable("ci.github.releaseLabels");

    if(string.IsNullOrEmpty(githubCompanyName))
        throw new Exception("githubCompanyName");

    if(string.IsNullOrEmpty(githubRepositoryName))
        throw new Exception("githubRepositoryName");        

    if(string.IsNullOrEmpty(githubUserName))
        throw new Exception("githubUserName");        

    if(string.IsNullOrEmpty(githubUserPassword))
        throw new Exception("githubUserPassword");        

    if(string.IsNullOrEmpty(releaseLabels))
        throw new Exception("releaseLabels");        

    if(String.IsNullOrEmpty(releaseTitle))
    {
        releaseTitle = System.IO.Path.GetFileNameWithoutExtension(defaultSolutionFilePath);
    }

    if(String.IsNullOrEmpty(releaseVersion) && defaultNuspecs.Count > 0)
    {
        var nuSpec = defaultNuspecs.First();
        
        var id = nuSpec.Id; 
        var version = nuSpec.Version; 

        if(ciBranch != "master")
            releaseVersion = GetVersionForNuGetPackage(id, "beta");
        else
            releaseVersion = GetVersionForNuGetPackage(id, ciBranch);
    }

    Information(String.Format("-githubCompanyName:[{0}]",githubCompanyName));
    Information(String.Format("-githubRepositoryName:[{0}]", githubRepositoryName));

    Information(String.Format("-releaseTemplateFileName:[{0}]", releaseTemplateFileName));
    Information(String.Format("-releaseLabels:[{0}]", releaseLabels));

    var releaseNotesFilePath = CreateGitHubReleaseNotes(
        githubCompanyName,
        githubRepositoryName,

        githubUserName,
        githubUserPassword,

        releaseTitle,
        releaseVersion,

        releaseAssemblyFileVersion,

        releaseTemplateFileName,
        releaseLabels
    );
});

// Action-XXX - common tasks
// * Action-Validate-Environment
// * Action-Clean
// * Action-Restore-NuGet-Packages
// * Action-Build
// * Action-Run-Unit-Tests

// * Action-API-NuGet-Packaging
// * Action-API-NuGet-Publishing

// * Action-CLI-Zip-Packaging
// * Action-CLI-Zip-Publishing

// * Action-CLI-Chocolatey-Packaging
// * Action-CLI-Chocolatey-Publishing

// * Action-GitHub-ReleaseNotes

// basic common targets
// expose them as global vars by naming conventions
// later, a particular build script can 'attach' additional tasks
// such as Pester regression testing on console app and so on
var taskDefault = Task("Default")
    .IsDependentOn("Default-Run-UnitTests");

var taskDefaultClean = Task("Default-Clean")
    .IsDependentOn("Action-Validate-Environment")
    .IsDependentOn("Action-Clean");

var taskDefaultBuild = Task("Default-Build")
    .IsDependentOn("Default-Clean")
    .IsDependentOn("Action-Build");

var taskDefaultRunUnitTests = Task("Default-Run-UnitTests")
    .IsDependentOn("Default-Build")
    .IsDependentOn("Action-Run-UnitTests");    

// API related targets
var taskDefaultAPINuGetPackaging = Task("Default-API-NuGet-Packaging")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging");

var taskDefaultAPINuGetPublishing = Task("Default-API-NuGet-Publishing")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging")
    .IsDependentOn("Action-API-NuGet-Publishing");  

// CLI related targets
var taskDefaultCLIPackaging = Task("Default-CLI-Packaging")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging")

    .IsDependentOn("Action-CLI-Zip-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Packaging");  

Task("Default-CLI-Publishing")
    .IsDependentOn("Default-CLI-Packaging");

// CI related targets
var taskDefaultCI = Task("Default-CI")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging")
    .IsDependentOn("Action-CLI-Zip-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Packaging")
    // always 'push'
    // the action checks if the current branch has to be published (dev always, the rest goes via 'ci.nuget.shouldpublish')
    .IsDependentOn("Action-API-NuGet-Publishing")
	
	// always create a new release - either in draft or published as per 'ci.github.shouldpublish' variable
	.IsDependentOn("Action-GitHub-ReleaseNotes")

	.IsDependentOn("Action-Docs-Merge");
