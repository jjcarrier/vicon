<#
.DESCRIPTION
Builds release artifacts. Requires dotnet and inno-setup on system path.
#>
param(
    [ArgumentCompletions('win-x64', 'linux-x64', 'linux-arm64', 'linux-arm', 'osx-x64', 'osx-arm64')]
    [Parameter()]
    [string[]]$Runtimes = @('win-x64', 'linux-x64', 'linux-arm64', 'linux-arm', 'osx-x64', 'osx-arm64'),

    # Version string (i.e. "0.0.0")
    # If specified it will update the assembly version for the program prior to compilation with the version
    # specified. Otherwise, the version will be extracted from app/PowerControllerApp.csproj
    [Parameter()]
    [version]$Version
)

$artifactsPath = "./artifacts"
$standAloneBuildPath = "./build-standalone"
$buildPath = "./build"
$csprojPath = Join-Path -Path $PSScriptRoot -ChildPath "../app/PowerControllerApp.csproj"

function Get-ProjectVersion {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Error "Project file not found: $Path"
        exit 1
    }

    [xml]$xml = Get-Content -Path $Path
    $fileVersion = $xml.Project.PropertyGroup.FileVersion | Select-Object -First 1
    $assemblyVersion = $xml.Project.PropertyGroup.AssemblyVersion | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($fileVersion)) {
        if ([string]::IsNullOrWhiteSpace($assemblyVersion)) {
            Write-Error "No FileVersion or AssemblyVersion found in $Path"
            exit 1
        }
        return $assemblyVersion
    }

    return $fileVersion
}

function Set-ProjectVersion {
    param(
        [string]$Path,
        [string]$NewVersion
    )

    if (-not (Test-Path $Path)) {
        Write-Error "Project file not found: $Path"
        exit 1
    }

    [xml]$xml = Get-Content -Path $Path

    $propertyGroup = $xml.Project.PropertyGroup | Select-Object -First 1
    if (-not $propertyGroup) {
        $propertyGroup = $xml.CreateElement("PropertyGroup")
        $xml.Project.AppendChild($propertyGroup) | Out-Null
    }

    if (-not $propertyGroup.FileVersion) {
        $fileVersionNode = $xml.CreateElement("FileVersion")
        $propertyGroup.AppendChild($fileVersionNode) | Out-Null
    }
    if (-not $propertyGroup.AssemblyVersion) {
        $assemblyVersionNode = $xml.CreateElement("AssemblyVersion")
        $propertyGroup.AppendChild($assemblyVersionNode) | Out-Null
    }

    $propertyGroup.FileVersion = $NewVersion
    $propertyGroup.AssemblyVersion = $NewVersion

    $xml.Save($Path)
}

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

    dotnet publish "../app/PowerControllerApp.csproj" $scOption --configuration Release --runtime $Runtime --output $outPath `
        -p:Version=$Version `
        -p:AssemblyVersion=$Version `
        -p:FileVersion=$Version `
        -p:InformationalVersion=$Version

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

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    Set-ProjectVersion -Path $csprojPath -NewVersion $Version
} else {
    $Version = Get-ProjectVersion -Path $csprojPath
}

$Runtimes | ForEach-Object {
    Write-Output "Publishing: $Version for $_ ..."
    Publish-Artifact -Runtime $_
    if ($_ -eq 'win-x64') {
        $archiveType = "zip"
        Compress-Archive -Path "$buildPath/$_/*" -DestinationPath "$artifactsPath/vicon-$Version-$_.$archiveType"
    } else {
        $archiveType = "tgz"
        tar -czf "$artifactsPath/vicon-$Version-$_.$archiveType" `
            -C "$buildPath/$_" . `
            -C "$PSScriptRoot/.." udev
    }
}

$Runtimes | ForEach-Object {
    Write-Output "Publishing (stand-alone): $Version for $_ ..."
    Publish-Artifact -Runtime $_ -SelfContained
    if ($_ -eq 'win-x64') {
        $archiveType = "zip"
        Compress-Archive -Path "$standAloneBuildPath/$_/*" -DestinationPath "$artifactsPath/vicon-$Version-$_-standalone.$archiveType"
    } else {
        $archiveType = "tgz"
        tar -czf "$artifactsPath/vicon-$Version-$_-standalone.$archiveType" `
            -C "$standAloneBuildPath/$_" . `
            -C "$PSScriptRoot/.." udev
    }
}

Pop-Location
