#Requires -Version 7.0

# Builds, tests and packs the solution. Works on Windows, Linux and macOS.
# When the NUGET_SOURCE and NUGET_API_KEY environment variables are set, packages are also pushed.

Begin {
    $ErrorActionPreference = "stop"
}

Process {
    function Exec([scriptblock]$Command) {
        & $Command
        if ($LASTEXITCODE -ne 0) {
            throw ("An error occurred while executing command: {0}" -f $Command)
        }
    }

    $solution = Join-Path $PSScriptRoot "Askaiser.Marionette.slnx"
    $outputDir = Join-Path $PSScriptRoot ".output"
    $nupkgsPath = Join-Path $outputDir "*.nupkg"

    try {
        Push-Location $PSScriptRoot
        Remove-Item $outputDir -Force -Recurse -ErrorAction SilentlyContinue

        Exec { & dotnet clean $solution -c Release }
        Exec { & dotnet build $solution -c Release }
        Exec { & dotnet test --solution $solution -c Release --no-build }
        Exec { & dotnet pack $solution -c Release -o "$outputDir" --no-build }

        if (($null -ne $env:NUGET_SOURCE) -and ($null -ne $env:NUGET_API_KEY) -and ($env:NUGET_SOURCE -ne "") -and ($env:NUGET_API_KEY -ne "")) {
            Exec { & dotnet nuget push "$nupkgsPath" -s $env:NUGET_SOURCE -k $env:NUGET_API_KEY --skip-duplicate }
        }
    }
    finally {
        Pop-Location
    }
}
