// locks on versions so that it won't break with new releases

#addin nuget:https://www.nuget.org/api/v2/?package=Cake.Powershell&Version=0.4.7
#addin nuget:https://www.nuget.org/api/v2/?package=newtonsoft.json&Version=12.0.1
#addin nuget:https://www.nuget.org/api/v2/?package=NuGet.Core&Version=2.14.0
#addin nuget:https://www.nuget.org/api/v2/?package=Cake.WebDeploy&Version=0.3.3

var version = "0.2.1902-beta2";

Information("Running SubPointSolutions.CakeBuildTools: " + version);

Setup(ctx => {
	Information("Running SubPointSolutions.CakeBuildTools: " + version);
});

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

         // master always builds the major version removing '-beta' postfix
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

string GetSafeConfigValue(string name, string defaultValue) {

    var value = jsonConfig[name];

    if(value == null)
        return defaultValue;

    return (string)value;
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

// VS?
ciBranchOverride = GetGlobalEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
if(!String.IsNullOrEmpty(ciBranchOverride))
{
    Information(String.Format("Detected VS Online build. Reverting to BUILD_SOURCEBRANCHNAME varibale:[{0}]", ciBranchOverride));
	ciBranch = ciBranchOverride;
}

var ciNuGetSource = GetGlobalEnvironmentVariable("ci.nuget.source") ?? String.Empty;
var ciNuGetKey = GetGlobalEnvironmentVariable("ci.nuget.key") ?? String.Empty;

var ciNuGetShouldPublish = bool.Parse(GetGlobalEnvironmentVariable("ci.nuget.shouldpublish") ?? "FALSE");
var ciChocolateyShouldPublish = bool.Parse(GetGlobalEnvironmentVariable("ci.chocolatey.shouldpublish") ?? "FALSE");
var ciWebDeployShouldPublish = bool.Parse(GetGlobalEnvironmentVariable("ci.webdeploy.shouldpublish") ?? "FALSE");

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

// web deploy settings
//var defaultWebDeploySettings = jsonConfig["defaultWebDeploySettings"];
var defaultWebDeployPackageDir = GetFullPath(GetSafeConfigValue("defaultWebDeployPackageDir", "./build-artifact-webdeploy"));
var defaultWebDeployTmpPackageDir = GetFullPath(GetSafeConfigValue("defaultWebDeployTmpPackageDir", "./build-artifact-webdeploy-tmp"));

System.IO.Directory.CreateDirectory(defaultWebDeployPackageDir);
System.IO.Directory.CreateDirectory(defaultWebDeployTmpPackageDir);

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

// add web deploy dirs to clean folder
defaultBuildDirs.Add(defaultWebDeployPackageDir);
defaultBuildDirs.Add(defaultWebDeployTmpPackageDir);

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

	var customProjectBuildProfiles = jsonConfig["customProjectBuildProfiles"];

	if(customProjectBuildProfiles == null || customProjectBuildProfiles.Count() == 0)
    {
        Verbose("No custom project profiles detected. Switching to normal *.sln build");
        
		MSBuild(defaultSolutionFilePath, settings => {
            settings.SetVerbosity(Verbosity.Quiet);
            settings.SetConfiguration(configuration);
		});

		return;
    }

    var currentBuildProfileIndex = 0;
    var buildProfilesCount = customProjectBuildProfiles.Count();

    foreach(var buildProfile in customProjectBuildProfiles) {

        currentBuildProfileIndex++;
        var profileName = (string)buildProfile["ProfileName"];

        Information(string.Format("[{0}/{1}] Building project profile:[{2}]",
            new object[] {
                currentBuildProfileIndex,
                buildProfilesCount,
                profileName
            }));

        var projectFiles = buildProfile["ProjectFiles"].Select(p => (string)p);
        var buildParameters = buildProfile["BuildParameters"].Select(p => (string)p);

        var currentProjectFileIndex = 0;
        var projecFilesCount = projectFiles.Count();

        foreach(var projectFile in projectFiles)
        {
            currentProjectFileIndex++;
            var fullProjectFilePath = ResolveFullPathFromSolutionRelativePath(projectFile);

            Information(string.Format(" [{0}/{1}] Building project file:[{2}]",
                new object[] {
                    currentProjectFileIndex,
                    projecFilesCount,
                    projectFile
            }));

            Verbose(string.Format(" - file path:[{0}]", fullProjectFilePath)); 
            
            var buildSettings =  new MSBuildSettings{

            };

            var buildParametersString = String.Empty;
            var solutionDirectoryParam = "/p:SolutionDir=" + defaultSolutionDirectory;

            buildParametersString += " " + solutionDirectoryParam;
            buildParametersString += " " + String.Join(" ", buildParameters);

            buildSettings.ArgumentCustomization = args => {
                
                foreach(var arg in buildParameters) {
                    args.Append(arg);
                }

                args.Append(solutionDirectoryParam);
                return args;
            };

            Verbose(string.Format(" - params:[{0}]", buildParametersString)); 
            MSBuild(fullProjectFilePath, buildSettings);
        }
    }        


     
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

bool ShouldPublishWebDeploy(string branch) {

    // always publish dev branch
    // the rest comes from 'ciWebDeployShouldPublish' -> 'ci.webdeploy.shouldpublish' environment variable
	if(branch == "dev")
		return true;

	return ciWebDeployShouldPublish;
}

bool ShouldPublishChocolatey(string branch) {
   
    // always publish dev branch
    // the rest comes from 'ciChocolateyShouldPublish' -> 'ci.chocolatey.shouldpublish' environment variable
	if(branch == "dev")
		return true;

	return ciChocolateyShouldPublish;
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



var defaultActionCLIChocolateyPublishing = Task("Action-CLI-Chocolatey-Publishing")
    .Does(() =>
{
        Information(String.Format("CLI Chocolatey publishing enabled? branch:[{0}]", ciBranch));

		var nuGetPackages = System.IO.Directory.GetFiles(defaultChocolateyPackagesDirectory, "*.nupkg");
        var shouldPublish = ShouldPublishChocolatey(ciBranch);

		Information(String.Format("shouldPublish?:[{0}]", shouldPublish));

		if(nuGetPackages.Count() == 0) {
			Information(String.Format("Can't find any choco packages. Returning...."));
			return;
		}

        var nugetSource = String.Empty;
        var nugetKey = String.Empty;

        if(!shouldPublish) {
            Information("Skipping Chocolatey publishing.");
            return;
        } else {
            Information("Fetching Chocolatey NuGet feed creds.");

            var feedSourceVariableName = String.Format("ci.chocolatey.{0}-source", ciBranch);
            var feedKeyVariableName = String.Format("ci.chocolatey.{0}-key", ciBranch);

            var feedSourceValue = GetGlobalEnvironmentVariable(feedSourceVariableName);
            var feedKeyValue = GetGlobalEnvironmentVariable(feedKeyVariableName);

            if(String.IsNullOrEmpty(feedSourceValue)) 
                throw new Exception(String.Format("environment variable is null or empty:[{0}]", feedSourceVariableName));

            if(String.IsNullOrEmpty(feedKeyValue)) 
                throw new Exception(String.Format("environment variable is null or empty:[{0}]", feedKeyVariableName));

            nugetSource = feedSourceValue;
            nugetKey = feedKeyValue;
        }

        Information("Publishing Chocolatey packages to repository: [{0}]", new []{
            nugetSource
        });

        

        foreach(var packageFilePath in nuGetPackages)
            {
                var packageFileName = System.IO.Path.GetFileName(packageFilePath);

                if(System.IO.File.Exists(packageFilePath)) {
                    
                    // checking is publushed
                    Information(string.Format("Checking if Chocolatey NuGet package [{0}] is already published", packageFileName));
                    
                    // TODO
                    var isNuGetPackagePublished = false;
                    if(!isNuGetPackagePublished)
                    {
                        Information(string.Format("Publishing Chocolatey NuGet package [{0}]...", packageFileName));
                    
                        ChocolateyPush(packageFilePath, new ChocolateyPushSettings {
                            Source = nugetSource,
                            ApiKey = nugetKey
                        });
                    }
                    else
                    {
                        Information(string.Format("Chocolatey NuGet package [{0}] was already published", packageFileName));
                    }                 
                    
                } else {
                    Information(string.Format("Chocolatey NuGet package does not exist:[{0}]", packageFilePath));
                    throw new ArgumentException(string.Format("Chocolatey NuGet package does not exist:[{0}]", packageFilePath));
                }
            }           
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

           if(string.IsNullOrEmpty(originalFileBase))
                originalFileBase = System.IO.Path.GetDirectoryName(files.FirstOrDefault(f => f.Contains(".dll")));

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

var defaultActionWebAppPackage = Task("Action-WebApp-Packaging")
    .Does(() =>
{
    if(jsonConfig["defaultWebDeploySettings"] == null)
    {
        Information("defaultWebDeploySettings section in JSON file is null. Skipping step...");
        return;
    }

    var webDeploySettings = jsonConfig["defaultWebDeploySettings"];

	Information(string.Format("Building web deploy packages [{1}] in folder:[{0}]",
             defaultWebDeployPackageDir,
             webDeploySettings.Count())); 

    foreach(var webDeploySetting in webDeploySettings)
    {
        var csProjectFilePath = ResolveFullPathFromSolutionRelativePath((string)webDeploySetting["SolutionRelativeProjectFilePath"]);
        var csProjectFileName = System.IO.Path.GetFileNameWithoutExtension(csProjectFilePath);

        var siteFolderName = csProjectFileName;

        Information(string.Format("Building web deploy package for file path:[{0}]",
                csProjectFilePath));

        if(!System.IO.File.Exists(csProjectFilePath))
            throw new Exception(string.Format("Cannot find project file:[{0}]", csProjectFilePath));

        var siteDirectoryPath = System.IO.Path.Combine(defaultWebDeployPackageDir, siteFolderName);
        System.IO.Directory.CreateDirectory(siteDirectoryPath);

        Information(String.Format("Cleaning directory:[{0}]", siteDirectoryPath));
        System.IO.Directory.Delete(siteDirectoryPath, true);
        System.IO.Directory.CreateDirectory(siteDirectoryPath);

        Information(String.Format("Creating web deploy package in directory:[{0}]", siteDirectoryPath));
        MSBuild(csProjectFilePath, settings =>
            settings
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithTarget("WebPublish")
            .WithProperty("Verbosity", "Silent")
            .WithProperty("VisualStudioVersion", new string[]{"14.0"})
            .WithProperty("WebPublishMethod", new string[]{ "FileSystem" })
            .WithProperty("PublishUrl", new string[]{ siteDirectoryPath })
            );
    }   
});

var defaultActionWebAppDeploy = Task("Action-WebApp-Publishing")
    .Does(() =>
{
    if(jsonConfig["defaultWebDeploySettings"] == null)
    {
        Information("defaultWebDeploySettings section in JSON file is null. Skipping step...");
        return;
    }

    var shouldPublish = ShouldPublishAPINuGet(ciBranch);

    if(!shouldPublish)
    {
        Information(string.Format("Skipping web deploy publishing, shouldPublish = false"));
        return;
    }

    if(jsonConfig["defaultWebDeploySettings"] == null)
        throw new Exception("defaultWebDeploySettings section in JSON file is null");

    var webDeploySettings = jsonConfig["defaultWebDeploySettings"];

	Information(string.Format("Building web deploy packages [{1}] in folder:[{0}]",
             defaultWebDeployPackageDir,
             webDeploySettings.Count())); 

    foreach(var webDeploySetting in webDeploySettings)
    {
        var csProjectFilePath = ResolveFullPathFromSolutionRelativePath((string)webDeploySetting["SolutionRelativeProjectFilePath"]);
        var csProjectFileName = System.IO.Path.GetFileNameWithoutExtension(csProjectFilePath);
        
        var siteFolderName = csProjectFileName;
        var siteDirectoryPath = System.IO.Path.Combine(defaultWebDeployPackageDir, siteFolderName);

        var webDeploySiteName = String.Empty;
        var webDeploySiteUserPassword = String.Empty;

        {
            Information("Fetching creds.");

            var siteNamedVariableName = String.Format("ci.webdeploy.{0}-{1}.sitename", csProjectFileName, ciBranch);
            var sitePasswordVariableName = String.Format("ci.webdeploy.{0}-{1}.sitepassword", csProjectFileName, ciBranch);

            var siteNameValue = GetGlobalEnvironmentVariable(siteNamedVariableName);
            var sitePasswordValue = GetGlobalEnvironmentVariable(sitePasswordVariableName);

            if(String.IsNullOrEmpty(siteNameValue)) 
                throw new Exception(String.Format("environment variable is null or empty:[{0}]", siteNamedVariableName));

            if(String.IsNullOrEmpty(sitePasswordValue)) 
                throw new Exception(String.Format("environment variable is null or empty:[{0}]", sitePasswordVariableName));

            webDeploySiteName = siteNameValue;
            webDeploySiteUserPassword = sitePasswordValue;
        }

        Information(string.Format("Publishing web site [{0}] from folder:[{1}]",
            webDeploySiteName,
            siteDirectoryPath));	
        
        DeployWebsite(new DeploySettings()
        {
                SourcePath = siteDirectoryPath,
                SiteName = webDeploySiteName,
                ComputerName = "https://" + webDeploySiteName + ".scm.azurewebsites.net:443/msdeploy.axd?site=" + webDeploySiteName,
                Username = "$" + webDeploySiteName,
                Password = webDeploySiteUserPassword
        });
    }        
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

// * Action-WebApp-Packaging
// * Action-WebApp-Publishing

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
	.IsDependentOn("Action-Restore-NuGet-Packages")
    .IsDependentOn("Action-Build")
    .IsDependentOn("Action-WebApp-Packaging");

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
    .IsDependentOn("Action-CLI-Chocolatey-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Publishing");  

var defaultCLIPublishing = Task("Default-CLI-Publishing")
    .IsDependentOn("Action-CLI-Zip-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Publishing");

var defaultWebAppPublishing = Task("Default-WebApp-Publishing")
    .IsDependentOn("Action-WebApp-Publishing");

// CI related targets
var taskDefaultCI = Task("Default-CI")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging")

    .IsDependentOn("Action-CLI-Zip-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Packaging")

    // always 'push'
    // the action checks if the current branch has to be published 
    // (dev always, the rest goes via 'ci.nuget.shouldpublish' / 'ci.chocolatey.shouldpublish')
    
    .IsDependentOn("Action-API-NuGet-Publishing")
    .IsDependentOn("Action-CLI-Chocolatey-Publishing")
    .IsDependentOn("Action-WebApp-Publishing");

var taskDefaultCILocal = Task("Default-CI-Local")
    .IsDependentOn("Default-Run-UnitTests")
    .IsDependentOn("Action-API-NuGet-Packaging")

    .IsDependentOn("Action-CLI-Zip-Packaging")
    .IsDependentOn("Action-CLI-Chocolatey-Packaging");