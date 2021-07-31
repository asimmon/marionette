using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cake.Codecov;
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
using Cake.Common.Tools.ReportGenerator;
using Cake.Core;
using Cake.Coverlet;
using Cake.Frosting;
using Path = System.IO.Path;

[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleType", Justification = "Multiple tasks classes in a single file improves readability.")]

public static class Program
{
    public static int Main(string[] args) => new CakeHost()
        .InstallTool(new Uri("nuget:?package=GitVersion.CommandLine&version=5.6.10"))
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
        this.MsBuildConfiguration = context.Argument("configuration", Constants.Release);
        this.SharedMSBuildSettings = new DotNetCoreMSBuildSettings();

        if (Guid.TryParse(context.Argument("codecov-token", string.Empty), out var codecovToken))
        {
            context.Information("Code coverage is enabled.");
            this.CodecovToken = codecovToken;
        }
    }

    public string MsBuildConfiguration { get; }

    public DotNetCoreMSBuildSettings SharedMSBuildSettings { get; }

    public Guid? CodecovToken { get; }

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
        if (context.Argument("package-version", string.Empty).Trim() is { Length: > 0 } userVersion)
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
            var informationalVersion = gitVersion.AssemblySemVer + "-" + gitVersion.Sha;

            context.Information("Generated assembly version: " + gitVersion.AssemblySemVer);
            context.Information("Generated file version: " + gitVersion.AssemblySemFileVer);
            context.Information("Generated informational version: " + informationalVersion);

            if (context.HasArgument("alpha"))
            {
                context.AddMSBuildProperty("VersionPrefix", gitVersion.AssemblySemVer);
                context.AddMSBuildProperty("VersionSuffix", "alpha" + gitVersion.CommitsSinceVersionSource.GetValueOrDefault(0).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                context.AddMSBuildProperty("Version", gitVersion.AssemblySemVer);
            }

            context.AddMSBuildProperty("FileVersion", gitVersion.AssemblySemFileVer);
            context.AddMSBuildProperty("InformationalVersion", informationalVersion);
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
        context.CleanDirectories(Constants.CoverageDirectoryPath);
    }
}

[TaskName("Restore")]
[TaskDescription("Restore nuget packages for the whole solution.")]
[IsDependentOn(typeof(MetadataTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetCoreRestore(Constants.SolutionPath, new DotNetCoreRestoreSettings
    {
        MSBuildSettings = context.SharedMSBuildSettings,
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
        NoLogo = true,
    });
}

[TaskName("Test")]
[TaskDescription("Run discovered tests inside the whole solution.")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var testSettings = new DotNetCoreTestSettings
        {
            Configuration = context.MsBuildConfiguration,
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

            if (context.CodecovToken.HasValue)
            {
                coverletSettings.CollectCoverage = true;
                coverletSettings.CoverletOutputFormat = CoverletOutputFormat.cobertura;
                coverletSettings.CoverletOutputDirectory = coverageDirectoryPath;
                coverletSettings.CoverletOutputName = outputFileName;
                coverletSettings.IncludeTestAssembly = false;
            }

            context.DotNetCoreTest(testProjectFilePath, testSettings, coverletSettings);
        }

        if (context.CodecovToken.HasValue)
        {
            var codecovToken = context.CodecovToken.Value;

            var inputCoverageFilePaths = context.GetFiles(Path.Combine(Constants.CoverageDirectoryPath, "*" + outputFileExtension));
            var outputCoverageFilePath = Path.Combine(Constants.CoverageDirectoryPath, "Cobertura.xml");

            context.ReportGenerator(inputCoverageFilePaths, coverageDirectoryPath, new ReportGeneratorSettings
            {
                ReportTypes = new[] { ReportGeneratorReportType.Cobertura },
            });

            context.Codecov(outputCoverageFilePath, codecovToken.ToString("D"));
        }
    }
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
        NoBuild = false, // required to also pack the already built source generator DLL
        NoRestore = true,
        NoLogo = true,
    });
}

[TaskName("Default")]
[IsDependentOn(typeof(PackTask))]
public class DefaultTask : FrostingTask
{
}
