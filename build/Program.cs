using System;
using System.Diagnostics.CodeAnalysis;
using Cake.Codecov;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.ReportGenerator;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Coverlet;
using Cake.Frosting;
using Path = System.IO.Path;

[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleType", Justification = "Multiple tasks classes in a single file improves readability.")]

public static class Program
{
    public static int Main(string[] args) => new CakeHost()
        .InstallTool(new Uri("dotnet:?package=GitVersion.Tool&version=5.10.1"))
        .InstallTool(new Uri("nuget:?package=ReportGenerator&version=4.8.12"))
        .InstallTool(new Uri("nuget:?package=Codecov&version=1.13.0"))
        .UseContext<BuildContext>()
        .Run(args);
}

public static class Constants
{
    public const string Release = "Release";
    public const string ProjectName = "Askaiser.Marionette";

    public static readonly string SourceDirectoryPath = Path.Combine("..", "src");
    public static readonly string OutputDirectoryPath = Path.Combine("..", ".output");
    public static readonly string CoverageDirectoryPath = Path.Combine("..", ".coverage");
    public static readonly string SolutionPath = Path.Combine("..", ProjectName + ".sln");
    public static readonly string MainProjectPath = Path.Combine(SourceDirectoryPath, ProjectName, ProjectName + ".csproj");
}

public class BuildContext : FrostingContext
{
    public BuildContext(ICakeContext context)
        : base(context)
    {
        this.MSBuildSettings = new DotNetMSBuildSettings();

        this.NugetApiKey = context.Argument("nuget-api-key", string.Empty);
        this.NugetSource = context.Argument("nuget-source", string.Empty);
        this.CodecovToken = context.Argument("codecov-token", string.Empty);

        if (!string.IsNullOrEmpty(this.CodecovToken))
        {
            context.Information("Code coverage is enabled.");
        }
    }

    public DotNetMSBuildSettings MSBuildSettings { get; }

    public string CodecovToken { get; }

    public string NugetApiKey { get; }

    public string NugetSource { get; }

    public void AddMSBuildSetting(string name, string value, bool log = false)
    {
        if (log)
        {
            this.Log.Information(name + ": " + value);
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            this.MSBuildSettings.Properties[name] = new[] { value };
        }
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var objGlobPath = Path.Combine(Constants.SourceDirectoryPath, "*", "obj");
        var binGlobPath = Path.Combine(Constants.SourceDirectoryPath, "*", "bin");

        context.CleanDirectories(Constants.OutputDirectoryPath);
        context.CleanDirectories(objGlobPath);
        context.CleanDirectories(binGlobPath);
        context.CleanDirectories(Constants.CoverageDirectoryPath);
    }
}

[TaskName("GitVersion")]
public sealed class GitVersionTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var gitVersion = context.GitVersion();

        context.AddMSBuildSetting("Version", gitVersion.NuGetVersion, log: true);
        context.AddMSBuildSetting("VersionPrefix", gitVersion.MajorMinorPatch, log: true);
        context.AddMSBuildSetting("VersionSuffix", gitVersion.PreReleaseTag, log: true);
        context.AddMSBuildSetting("PackageVersion", gitVersion.FullSemVer, log: true);
        context.AddMSBuildSetting("InformationalVersion", gitVersion.InformationalVersion, log: true);
        context.AddMSBuildSetting("AssemblyVersion", gitVersion.AssemblySemVer, log: true);
        context.AddMSBuildSetting("FileVersion", gitVersion.AssemblySemFileVer, log: true);
        context.AddMSBuildSetting("RepositoryBranch", gitVersion.BranchName, log: true);
        context.AddMSBuildSetting("RepositoryCommit", gitVersion.Sha, log: true);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(GitVersionTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetRestore(Constants.SolutionPath, new DotNetRestoreSettings
    {
        MSBuildSettings = context.MSBuildSettings,
    });
}

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.AddMSBuildSetting("Deterministic", "true");
        context.AddMSBuildSetting("ContinuousIntegrationBuild", "true");

        context.DotNetBuild(Constants.SolutionPath, new DotNetBuildSettings
        {
            Configuration = Constants.Release,
            MSBuildSettings = context.MSBuildSettings,
            NoRestore = true,
            NoLogo = true,
        });
    }
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var testSettings = new DotNetCoreTestSettings
        {
            Configuration = Constants.Release,
            NoBuild = true,
            NoLogo = true,
        };

        var coverageDirectoryPath = context.Directory(Constants.CoverageDirectoryPath);
        var testProjectGlobPath = Path.Combine(Constants.SourceDirectoryPath, "*", "*.Tests.csproj");
        var testProjectFilePaths = context.GetFiles(testProjectGlobPath);

        const string outputFileExtension = ".xml";

        foreach (var testProjectFilePath in testProjectFilePaths)
        {
            var outputFileName = testProjectFilePath.GetFilenameWithoutExtension().ToString().Replace('.', '-').ToLowerInvariant() + outputFileExtension;

            var coverletSettings = new CoverletSettings
            {
                CollectCoverage = false,
            };

            coverletSettings.CollectCoverage = true;
            coverletSettings.CoverletOutputFormat = CoverletOutputFormat.cobertura;
            coverletSettings.CoverletOutputDirectory = coverageDirectoryPath;
            coverletSettings.CoverletOutputName = outputFileName;
            coverletSettings.IncludeTestAssembly = false;

            context.DotNetCoreTest(testProjectFilePath, testSettings, coverletSettings);
        }

        if (!string.IsNullOrEmpty(context.CodecovToken))
        {
            var inputCoverageFilePaths = context.GetFiles(Path.Combine(Constants.CoverageDirectoryPath, "*" + outputFileExtension));
            var outputCoverageFilePath = Path.Combine(Constants.CoverageDirectoryPath, "Cobertura.xml");

            context.ReportGenerator(inputCoverageFilePaths, coverageDirectoryPath, new ReportGeneratorSettings
            {
                ReportTypes = new[] { ReportGeneratorReportType.Cobertura },
            });

            context.Codecov(outputCoverageFilePath, context.CodecovToken);
        }
    }
}

[TaskName("Pack")]
[IsDependentOn(typeof(TestTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetPack(Constants.MainProjectPath, new DotNetPackSettings
    {
        Configuration = Constants.Release,
        MSBuildSettings = context.MSBuildSettings,
        OutputDirectory = Constants.OutputDirectoryPath,
        NoBuild = false, // required to also pack the already built source generator DLL
        NoRestore = true,
        NoLogo = true,
    });
}

[TaskName("Push")]
[IsDependentOn(typeof(PackTask))]
public sealed class PushTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var packageFilePath in context.GetFiles(Path.Combine(Constants.OutputDirectoryPath, "*.nupkg")))
        {
            context.DotNetNuGetPush(packageFilePath, new DotNetNuGetPushSettings
            {
                ApiKey = context.NugetApiKey,
                Source = context.NugetSource,
                IgnoreSymbols = false,
            });
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackTask))]
public class DefaultTask : FrostingTask
{
}
