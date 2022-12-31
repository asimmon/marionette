#Requires -Version 5.0

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

    $workingDir = $PSScriptRoot
    $outputDir = Join-Path $PSScriptRoot ".output"
    $nupkgsPath = Join-Path $outputDir "*.nupkg"

    try {
        Push-Location $workingDir
        Remove-Item $outputDir -Force -Recurse -ErrorAction SilentlyContinue

        Exec { & dotnet clean -c Release }
        Exec { & dotnet build -c Release }
        Exec { & dotnet test  -c Release -r "$outputDir" --no-restore --no-build -l "trx" -l "console;verbosity=detailed" }
        Exec { & dotnet pack  -c Release -o "$outputDir" --no-restore }

        if (($null -ne $env:NUGET_SOURCE ) -and ($null -ne $env:NUGET_API_KEY)) {
            Exec { & dotnet nuget push "$nupkgsPath" -s $env:NUGET_SOURCE -k $env:NUGET_API_KEY }
        }
    }
    finally {
        Pop-Location
    }
}
