#addin nuget:https://www.nuget.org/api/v2/?package=Cake.Wyam
#tool nuget:https://www.nuget.org/api/v2/?package=Wyam

public class WyamBuildProfile : BuildProfile {
   
    public List<string> inputDirs { get; set; }
    public string rootDir { get; set; }
    public int? previewPort { get; set; }
    public String configFilePath { get; set; }

    public String recipeName { get; set; }
    public String themeName { get; set; }

    public List<NuGetSettings> nugetPackages { get; set; }   
}

public class WyamBuildService : ConfigurableBuildService<WyamBuildProfile>
{   
    public WyamBuildService()
    {
        Init();
    }

    protected void Init() {
        Profiles = new List<WyamBuildProfile>();
        var profiles = ConfigService.Config["wyam"];
        
        Info("Loading wyamProfiles...");

        foreach(var profile in profiles) {
            var buildProfile = profile.ToObject<WyamBuildProfile>();
            Debug("profile: " + buildProfile.Name);
            Profiles.Add(buildProfile);
        }

        Info("Loading wyamProfiles completed!");
    }

    public void ActionWyamVerifyConfig() {

    }

    WyamSettings GetDefaultWyamSettings()
    {
        return GetDefaultWyamSettings(null);
    }

    WyamSettings GetDefaultWyamSettings(Action<WyamSettings> action)
    {
        var defaultWyamInputDirs = Profile.inputDirs;
        var defaultSolutionDirectory = ConfigService.SolutionDirectory;

        var wyamConfigFilePath = LookupWyamConfigFile();
        var wyamConfigFileDir = System.IO.Path.GetDirectoryName(wyamConfigFilePath);

        defaultWyamInputDirs.Add(wyamConfigFileDir);

        var targetFolderPaths = defaultWyamInputDirs.Select(p => System.IO.Path.Combine(defaultSolutionDirectory, p));

        var rootPath = System.IO.Path.Combine(defaultSolutionDirectory, Profile.rootDir);
        
        var recipeName =  Profile.recipeName;
        var themeName =  Profile.themeName;

        Debug(" - recipe: {0}", recipeName);
        Debug(" - theme: {0}", themeName);
        Debug(" - rootPath: {0}", rootPath);
        Debug(" - defaultWyamInputDirs: {0}", System.Environment.NewLine + String.Join(System.Environment.NewLine, targetFolderPaths));

        foreach (var targetFolderPath in targetFolderPaths)
        {
            if (!System.IO.Directory.Exists(targetFolderPath))
            {
                throw new Exception(String.Format("Path does not exist: {0}", targetFolderPath));
            }
        }

        var wyamSettings = new WyamSettings
        {
            RootPath = rootPath,
            InputPaths = targetFolderPaths.Select(d => new Cake.Core.IO.DirectoryPath(d)),
            Preview = false,
            Watch = false
        };

        if (!String.IsNullOrEmpty(recipeName))
            wyamSettings.Recipe = recipeName;
    
        if (!String.IsNullOrEmpty(themeName))
            wyamSettings.Theme = themeName;
    
        wyamSettings.ConfigurationFile = LookupWyamConfigFile();

        if (action != null)
        {
            action(wyamSettings);
        }

        return wyamSettings;
    }
    
    string GetTemporaryWyamThemeConfigFolder()
    {

        var path = "./build-artifact-wyam-themes-config-tmp";
        System.IO.Directory.CreateDirectory(path);

        return System.IO.Path.GetFullPath(path);
    }

    string LookupWyamConfigFile()
    {
        var result = Profile.configFilePath;
        var defaultWyamConfigFile = Profile.configFilePath;

        var themeConfigs = GetWyamThemeConfigs();

        if (themeConfigs.Keys.Contains(defaultWyamConfigFile))
        {

            result = themeConfigs[defaultWyamConfigFile];
            // we need to copy wyam config file into a save folder
            // original location could be overwritten with package restore (new theme update)
            // that triggers deletion Wyam's cachece and foo..packages.xml file
            // in turn, a full NuGet restore is run over all Wyam packages

            // hence, copying this file into _wyam_themes_config folde
            var srcFilePath = result;

            var dstFolderPath = GetTemporaryWyamThemeConfigFolder();
            var dstFilePath = dstFolderPath + "/" + System.IO.Path.GetFileName(srcFilePath);

            Debug("   - copying file");
            Debug("       - src: " + srcFilePath);
            Debug("       - dst: " + dstFilePath);

            System.IO.File.Copy(srcFilePath, dstFilePath, true);
            result = dstFilePath;

            Info("   - using custom, theme based-wyam config: " + result);
        }
        else
        {
            result = System.IO.Path.Combine(ConfigService.SolutionDirectory, defaultWyamConfigFile);

            Info("   - using default wyam config: " + result);
        }

        return result;
    }

    Dictionary<string, string> GetWyamThemeConfigs()
    {

        var result = new Dictionary<string, string>();

        PreInstallWyamPackages();

        var themeName = Profile.themeName;
        var themeDir = GetTemporaryWyamThemeFolder() + "/" + themeName;

        var configFolder = themeDir + "/content/.config";

        if (System.IO.Directory.Exists(configFolder))
        {
            Debug("Looking for theme based wyam configs: {0}", configFolder);

            var filePaths = System.IO.Directory.GetFiles(configFolder);
            var dstFolderPath = GetWyamOutputDirectory();

            foreach (var filePath in filePaths)
            {
                var key = System.IO.Path.GetFileName(filePath);
                var path = System.IO.Path.GetFullPath(filePath);

                Debug("mapping theme based config");
                Debug("   - key: " + key);
                Debug("   - path: " + path);

                result.Add(key, path);

            }
        }
        else
        {
            Debug("Looking for theme based wyam configs folder does not exist: {0}", configFolder);
        }

        return result;
    }

        
    void PreInstallWyamPackages()
    {
        var nugetPackages = GetWyamNuGetPackages();

        foreach (var nugetPackage in nugetPackages)
        {
            Info("Preinstalling Wyam's NuGet package: {0}", nugetPackage.Package);

            var packageName = nugetPackage.Package;
            var installSetting = new NuGetInstallSettings();

            installSetting.ExcludeVersion = true;
            installSetting.OutputDirectory = GetTemporaryWyamThemeFolder();

            if (nugetPackage.Source != null)
            {
                installSetting.Source = nugetPackage.Source.ToArray();
            }

            if (!String.IsNullOrEmpty(nugetPackage.Version))
            {
                installSetting.Version = nugetPackage.Version;
            }

            Info("   - running NuGetInstall...");
            CakeContext.NuGetInstall(packageName, installSetting);
        }
    }

    string GetTemporaryWyamThemeFolder()
    {
        var path = "./build-artifact-wyam-themes-tmp";

        System.IO.Directory.CreateDirectory(path);

        return System.IO.Path.GetFullPath(path);
    }


    public void ActionWyamPreview() {
        WithDebug("ActionWyamPreview", () => {
            var nugetPackages = GetWyamNuGetPackages();

            BuildWyam(GetDefaultWyamSettings(settings => {
                settings.Preview = true;
                settings.Watch = true;
                settings.NuGetPackages = nugetPackages;
            }));
        });
    }

    void BuildWyam(WyamSettings settings)
    {
        var defaultWyamPreviewPort = Profile.previewPort ?? 5080;
        BuildWyam(settings, defaultWyamPreviewPort, GetWyamOutputDirectory());
    }

    void BuildWyam(WyamSettings settings, int port, String outputPath)
    {
        settings.PreviewPort = port;
        settings.OutputPath = outputPath;

        Info(String.Format("Wyam output path: {0}", outputPath));

        CakeContext.Wyam(settings);
    }

    public string GetWyamOutputDirectory() {
        return GetFullPath("./build-artifact-wyam");
    }

    public void ActionWyam() {

    }
    
    protected List<NuGetSettings> GetWyamNuGetPackages()
    {
        if(Profile.nugetPackages == null) {
            return new List<NuGetSettings>();
        }

        return Profile.nugetPackages;
    }
}

var wyamService = new WyamBuildService();

wyamService.CakeContext = Context;
wyamService.ProfileName = Argument("wyamProfile", "default");

serviceContainer.RegisterService(
                    typeof(WyamBuildService), 
                    wyamService 
                );

// builds Wyam based project
var defaultActionWyam = Task("Action-Wyam")
    .Does(() =>
    {
        var service = serviceContainer.GetService<WyamBuildService>();
        service.ActionWyam();
    });

var defaultNetlifyPublish = Task("Action-NetlifyPublish")
    .Does(() =>
    {
        var service = serviceContainer.GetService<WyamBuildService>();
        //service.NetlifyPublish();
    });

var defaultActionWyamPreview = Task("Action-WyamPreview")
    .Does(() =>
    {
       var service = serviceContainer.GetService<WyamBuildService>();
       service.ActionWyamPreview();
    });

var defaultActionWyamVerifyConfig = Task("Action-WyamVerifyConfig")
    .Does(() =>
    {
       var service = serviceContainer.GetService<WyamBuildService>();
       service.ActionWyamVerifyConfig();
    });

var taskDefaultWyam = Task("Default-Wyam")
    .IsDependentOn("Action-Wyam");

var taskDefaultWyamPublish = Task("Default-WyamPublish")
    .IsDependentOn("Action-Wyam")
    .IsDependentOn("Action-NetlifyPublish");