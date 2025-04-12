<#
.DESCRIPTION
Builds release artifacts. Requires dotnet and inno-setup on system path.
#>
param(
    [ArgumentCompletions('win-x64', 'linux-x64', 'linux-arm64', 'linux-arm', 'osx-x64', 'osx-arm64')]
    [Parameter()]
    [string[]]$Runtimes = @('win-x64', 'linux-x64', 'linux-arm64', 'linux-arm', 'osx-x64', 'osx-arm64'),

    # Version string (i.e. "0.0.0")
    [Parameter(Mandatory)]
    [string]$Version
)

$artifactsPath = "./artifacts"
$standAloneBuildPath = "./build-standalone"
$buildPath = "./build"

function Publish-Artifact
{
    param(
        [string]$Runtime,
        [switch]$SelfContained
    )

    if ($SelfContained) {
        $outPath = Join-Path -Path $standAloneBuildPath -ChildPath $Runtime
        $scOption = '--self-contained'
    } else {
        $outPath = Join-Path -Path $buildPath -ChildPath $Runtime
        $scOption = $null
    }

    dotnet publish "../app/PowerControllerApp.csproj" $scOption --configuration Release --runtime $Runtime --output $outPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for runtime: $Runtime"
        exit $LASTEXITCODE
    }

    if ($Runtime -eq 'win-x64' -and -not($SelfContained)) {
        # Also build the installer.
        ISCC /DMyAppVersion=$Version "./build-installer.iss"
    }
}

Push-Location
Set-Location $PSScriptRoot

if (-not (Test-Path -Path $artifactsPath)) {
    New-Item -ItemType Directory -Path $artifactsPath | Out-Null
}

Get-ChildItem -Path $artifactsPath -Recurse | Remove-Item -Force

$Runtimes | ForEach-Object {
    Write-Output "Publishing: $_"
    Publish-Artifact -Runtime $_
    if ($_ -eq 'win-x64') {
        $archiveType = "zip"
        Compress-Archive -Path "$buildPath/$_/*" -DestinationPath "$artifactsPath/vicon-$Version-$_.$archiveType"
    } else {
        $archiveType = "tgz"
        tar -czf "$artifactsPath/vicon-$Version-$_.$archiveType" -C "$buildPath/$_" .
    }
}

$Runtimes | ForEach-Object {
    Write-Output "Publishing (stand-alone): $_"
    Publish-Artifact -Runtime $_ -SelfContained
    if ($_ -eq 'win-x64') {
        $archiveType = "zip"
        Compress-Archive -Path "$standAloneBuildPath/$_/*" -DestinationPath "$artifactsPath/vicon-$Version-$_-standalone.$archiveType"
    } else {
        $archiveType = "tgz"
        tar -czf "$artifactsPath/vicon-$Version-$_-standalone.$archiveType" -C "$standAloneBuildPath/$_" .
    }
}

Pop-Location
