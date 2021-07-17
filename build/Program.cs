using System;
using System.IO;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.GitVersion;
using Cake.Core;
using Cake.Frosting;

public static class Program
{
    public static int Main(string[] args) => new CakeHost()
        .InstallTool(new Uri("nuget:?package=GitVersion.CommandLine&version=5.6.10"))
        .UseContext<BuildContext>()
        .Run(args);
}

public static class Constants
{
    public const string Release = "Release";
    public const string ProjectName = "Askaiser.Marionette";

    public static readonly string SourceDirectoryPath = Path.Combine("..", "src");
    public static readonly string OutputDirectoryPath = Path.Combine("..", "output");
    public static readonly string SolutionPath = Path.Combine("..", ProjectName + ".sln");
    public static readonly string MainProjectPath = Path.Combine(SourceDirectoryPath, ProjectName, ProjectName + ".csproj");
}

public class BuildContext : FrostingContext
{
    public BuildContext(ICakeContext context) : base(context)
    {
        this.MsBuildConfiguration = context.Argument("configuration", Constants.Release);
        this.SharedMSBuildSettings = new DotNetCoreMSBuildSettings();
    }

    public string MsBuildConfiguration { get; }
    public DotNetCoreMSBuildSettings SharedMSBuildSettings { get; }

    public void AddMSBuildProperty(string name, string value)
    {
        this.SharedMSBuildSettings.Properties[name] = new[] { value };
    }
}

[TaskName("Metadata")]
[TaskDescription("Add MSBuild shared properties that will be used with dotnet restore, build and pack.")]
public sealed class MetadataTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.SharedMSBuildSettings.Properties.Count <= 0 && base.ShouldRun(context);
    }

    public override void Run(BuildContext context)
    {
        AddMSBuildAssemblyVersion(context);
        AddMSBuildPackageMetadata(context);
    }

    private static void AddMSBuildAssemblyVersion(BuildContext context)
    {
        if (context.Argument("package-version", "").Trim() is { Length: > 0 } userVersion)
        {
            if (userVersion.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: 2 } versionParts)
            {
                context.AddMSBuildProperty("VersionPrefix", versionParts[0]);
                context.AddMSBuildProperty("VersionSuffix", versionParts[1]);
            }
            else
            {
                context.AddMSBuildProperty("Version", context.Argument<string>("package-version"));
            }
        }
        else
        {
            var gitVersion = context.GitVersion();
            context.Information("Generated assembly version: " + gitVersion.AssemblySemVer);

            if (context.HasArgument("alpha"))
            {
                context.AddMSBuildProperty("VersionPrefix", gitVersion.AssemblySemVer);
                context.AddMSBuildProperty("VersionSuffix", "alpha" + gitVersion.BuildMetaData);
            }
            else context.AddMSBuildProperty("Version", gitVersion.AssemblySemVer);
        }
    }

    private static void AddMSBuildPackageMetadata(BuildContext context)
    {
        context.AddMSBuildProperty("Description", Constants.ProjectName + " is a test automation framework based on image and text recognition.");
        context.AddMSBuildProperty("Authors", "Anthony Simmon");
        context.AddMSBuildProperty("Owners", "Anthony Simmon");
        context.AddMSBuildProperty("PackageProjectUrl", "https://github.com/asimmon/askaiser-marionette");
        context.AddMSBuildProperty("Copyright", "Copyright © Anthony Simmon " + DateTime.UtcNow.Year);
        context.AddMSBuildProperty("PackageLicenseExpression", "GPL-3.0-or-later");
        context.AddMSBuildProperty("TreatWarningsAsErrors", "True");
    }
}

[TaskName("Clean")]
[TaskDescription("Delete generated files and directories that are not versioned, such as compiled binaries.")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var objGlobPath = Path.Combine(Constants.SourceDirectoryPath, "*", "obj");
        var binGlobPath = Path.Combine(Constants.SourceDirectoryPath, "*", "bin");

        context.CleanDirectories(Constants.OutputDirectoryPath);
        context.CleanDirectories(objGlobPath);
        context.CleanDirectories(binGlobPath);
    }
}

[TaskName("Restore")]
[TaskDescription("Restore nuget packages for the whole solution.")]
[IsDependentOn(typeof(MetadataTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetCoreRestore(Constants.SolutionPath, new DotNetCoreRestoreSettings
    {
        MSBuildSettings = context.SharedMSBuildSettings
    });
}

[TaskName("Build")]
[TaskDescription("Build the whole solution.")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetCoreBuild(Constants.SolutionPath, new DotNetCoreBuildSettings
    {
        Configuration = context.MsBuildConfiguration,
        MSBuildSettings = context.SharedMSBuildSettings,
        NoRestore = true,
        NoLogo = true
    });
}

[TaskName("Test")]
[TaskDescription("Run discovered tests inside the whole solution.")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetCoreTest(Constants.SolutionPath, new DotNetCoreTestSettings
    {
        Configuration = context.MsBuildConfiguration,
        NoBuild = true,
        NoLogo = true,
    });
}

[TaskName("Pack")]
[TaskDescription("Create a nuget package.")]
[IsDependentOn(typeof(TestTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetCorePack(Constants.MainProjectPath, new DotNetCorePackSettings
    {
        Configuration = context.MsBuildConfiguration,
        MSBuildSettings = context.SharedMSBuildSettings,
        OutputDirectory = Constants.OutputDirectoryPath,
        NoBuild = false, // required to pack the additional source generator
        NoRestore = true,
        NoLogo = true,
    });
}

[TaskName("Default")]
[IsDependentOn(typeof(PackTask))]
public class DefaultTask : FrostingTask
{
}
