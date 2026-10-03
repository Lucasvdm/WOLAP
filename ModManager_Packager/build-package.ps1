# Tells PowerShell if anything Errors to stop running the code
$ErrorActionPreference = "Stop"
# Folder conatining this packaging script
$packagerRoot = $PSScriptRoot
# Root folder of the WOLAP Repository
$repoRoot = Split-Path -Parent $packagerRoot
# Root to Patcher
$patchRoot = Join-Path $repoRoot -ChildPath patcher\WOLAP.DependencyPatcher
# Root to the LICENSES folder
$licensesRoot = Join-Path $packagerRoot "LICENSES"
# Root to the configurables folder
$configRoot = Join-Path $packagerRoot "configurables"
# Root to the soon-to-be generated temp folder
$tempRoot = Join-Path $packagerRoot "temp"
# Root to the soon-to-be generated output folder
$outputRoot = Join-Path $packagerRoot "output"
# Root to ref folder
$refRoot = Join-Path $packagerRoot "ref"
# path to main-mod WOLAP.csproj
$mMWOLAPProj = Join-Path $repoRoot "WOLAP.csproj"
# Path to main-mod WOLAP.dll
$mMWORelPath = Join-Path $repoRoot -ChildPath bin\Release\netstandard2.0\WOLAP.dll
# Path to patch WOLAP.DependencyPatcher.csproj
$paWOLAPProj = Join-Path $patchRoot "WOLAP.DependencyPatcher.csproj"
# Path to the .csproj used only for restoring NuGet dependencies
$nugetProj = Join-Path $packagerRoot "NuGetDependencies.csproj"
# Folder NuGet will use for the packages it downloads
$nugetPackagesRoot = Join-Path $tempRoot "packages"
# Path to patch WOLAP.DependencyPatcher.dll
$paWORelPath = Join-Path $patchRoot -ChildPath bin\Release\net35\WOLAP.DependencyPatcher.dll
# Path to all files in the configurables folder
$configObj = Get-ChildItem $configRoot
$configPath = $configObj.FullName
# Path to all Licenses in .\ModManager_Packager\LICENSES
$licensesObj = Get-ChildItem $licensesRoot
$licensesPaths = $LicensesObj.FullName
# Path to WOLAP mod License
$mMLicensePath = Join-Path $repoRoot "LICENSE"
# Path to Archipelago dll
$apPath = Join-Path $tempRoot "Archipelago.MultiClient.Net.dll"
# insert comment
$nSoftPath = Join-Path $tempRoot "Newtonsoft.Json.dll"
# inset comment
$mMBPPath = Join-Path $tempRoot "MonoMod.Backports.dll"
# insert comment
$mMILHPath = Join-Path $tempRoot "MonoMod.ILHelpers.dll"
$stagingRoot = Join-Path $outputRoot "staging"

$collectedDll = @(
    $apPath
    $nSoftPath
    $mMBPPath
    $mMILHPath
)
# Creates an array of variables for each Proj under $needProj
$csProjs = @(
    $mMWOLAPProj
    $paWOLAPProj
)
# Creates an array of variables for each Path that needs to be checked before we run the .csproj files to get the last files for the packager
$neededFiles = @(
    $configPath
    $licensesPaths
    $mMLicensePath
)
# Creates an array of variables for each Path that is required for the check at NuGet file getting under $neededFilesMid
$neededFilesMid = @(
    $mMWORelPath
    $paWORelPath
    $neededFiles
)

Write-Host "Checking required files before starting .dll creation"
# pre-check to see if all files EXCEPT for the files that need to be made in the .csproj are present
foreach($file in $neededFiles) {if ( -not(Test-Path $file)) {
    throw "Required file not found at $file"
}}
Write-Host "All required files in place"
Write-Host "Checking .csproj codes exist"
# redundant pre-check to see if all .csproj codes exist before Creating their .dll files
foreach($proj in $csProjs) {if ( -not(Test-Path $proj)) {
    throw ".csproj not found at $proj"
}}
Write-Host ".csproj code exists, creating WOLAP.dll and WOLAP.DependencyPatcher.dll"
# creates the .dll files from their respective .csproj files
foreach ($pro in $csProjs) {dotnet build $pro -c Release 
    if ($LASTEXITCODE) {
        throw "Project code at $pro failed, please check the code"
}}
Write-Host "WOLAP.dll and WOLAP.DependencyPatcher.dll have been successfully created"

Write-Host "Preparing temporary and output folders"

# Temp is entirely disposable, so remove any previous temp folder and recreate it
if (Test-Path $tempRoot) {
    Write-Host "Old temp folder found, clearing it"
    Remove-Item -Path $tempRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $tempRoot | Out-Null

# Output may contain completed .Zip files, so only create it if it does not exist
if (-not (Test-Path $outputRoot)) {
    Write-Host "Output folder not found, creating it"
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}
else {
    Write-Host "Output folder exists, keeping previous completed packages"
}

# Staging is disposable, so remove only the old staging folder
if (Test-Path $stagingRoot) {
    Write-Host "Old staging folder found, clearing it"
    Remove-Item -Path $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot | Out-Null

Write-Host "Temporary and output folders are ready"

# Checks for local files before we have NuGet grab all the other necessary .dll files
Write-Host "Check if needed files are all collected to this point"
foreach ($file3 in $neededFilesMid) {if ( -not(Test-Path $file3)) {
    throw "Required file not found at $file3"
}}
Write-Host "All required files found"
Write-Host "Getting required dependency files through NuGet"

# Uses NuGetDependencies.csproj to download the exact pinned package versions
dotnet restore $nugetProj --packages $nugetPackagesRoot

# dotnet is an external program, so manually check whether it succeeded
if ($LASTEXITCODE) {
    throw "NuGet dependency restore failed"
}

Write-Host "NuGet dependencies successfully restored"

# Paths to the DLL files inside NuGet's generated package folders
$apNugetPath = Join-Path $nugetPackagesRoot "archipelago.multiclient.net\6.7.1\lib\netstandard2.0\Archipelago.MultiClient.Net.dll"
$nSoftNugetPath = Join-Path $nugetPackagesRoot "archipelago.multiclient.net\6.7.1\lib\netstandard2.0\Newtonsoft.Json.dll"
$mMBPNugetPath = Join-Path $nugetPackagesRoot "monomod.backports\1.1.2\lib\netstandard2.0\MonoMod.Backports.dll"
$mMILHNugetPath = Join-Path $nugetPackagesRoot "monomod.ilhelpers\1.1.0\lib\netstandard2.0\MonoMod.ILHelpers.dll"

# Verify NuGet actually produced the files we expect before attempting to copy them
$nugetDlls = @(
    $apNugetPath
    $nSoftNugetPath
    $mMBPNugetPath
    $mMILHNugetPath
)

foreach ($nugetDll in $nugetDlls) {
    if (-not (Test-Path $nugetDll)) {
        throw "Expected NuGet dependency was not found at $nugetDll"
    }
}

Write-Host "Collecting required dependency DLL files"

# Copy only the DLLs WOLAP actually needs into the top level temp folder
Copy-Item -Path $apNugetPath -Destination $apPath -Force
Copy-Item -Path $nSoftNugetPath -Destination $nSoftPath -Force
Copy-Item -Path $mMBPNugetPath -Destination $mMBPPath -Force
Copy-Item -Path $mMILHNugetPath -Destination $mMILHPath -Force

Write-Host "Required dependency DLL files collected"



$neededFilesEnd = @(
    $neededFilesMid
    $collectedDll
)
Write-Host "Checking required files before Packaging"
# will run after the .csproj files create their release .dll files, checks if all required files for packaging are present and throws an error if not
foreach($file2 in $neededFilesEnd) {if ( -not(Test-Path $file2)) {
    throw "Required file not found at $file2"
}}
Write-Host "All required Files in place, starting Packaging"

# Root to the BepInEx folder inside the Thunderstore staging folder
$stageBepInExRoot = Join-Path $stagingRoot "BepInEx"

# Staging locations for the different BepInEx file types
$stagePluginsRoot = Join-Path $stageBepInExRoot "plugins"
$stagePatchersRoot = Join-Path $stageBepInExRoot "patchers"
$stageCoreRoot = Join-Path $stageBepInExRoot "core"

# Staging location for third-party licenses
$stageLicensesRoot = Join-Path $stagingRoot "LICENSES"

# Optional files a future maintainer may want copied directly into BepInEx
$extraBepInExRoot = Join-Path $refRoot -ChildPath "extra\BepInEx"

Write-Host "Creating Thunderstore package staging structure"

# Creates the folder structure required inside the final Thunderstore package
$stagingFolders = @(
    $stageBepInExRoot
    $stagePluginsRoot
    $stagePatchersRoot
    $stageCoreRoot
    $stageLicensesRoot
)

foreach ($stageFolder in $stagingFolders) {
    New-Item -ItemType Directory -Path $stageFolder -Force | Out-Null
}

Write-Host "Copying configurable Thunderstore files"

# Copies README.md, CHANGELOG.md, manifest.json and icon.png
# from configurables directly into the root of the package
Copy-Item -Path $configPath -Destination $stagingRoot -Force

Write-Host "Copying license files"

# Main WOLAP license goes at the root of the package
Copy-Item -Path $mMLicensePath -Destination $stagingRoot -Force

# Third-party licenses go inside LICENSES
Copy-Item -Path $licensesPaths -Destination $stageLicensesRoot -Force

Write-Host "Copying plugin files"

# Main WOLAP plugin
Copy-Item -Path $mMWORelPath -Destination $stagePluginsRoot -Force

# Archipelago client library
Copy-Item -Path $apPath -Destination $stagePluginsRoot -Force

Write-Host "Copying preloader patcher files"

# Dependency patcher
Copy-Item -Path $paWORelPath -Destination $stagePatchersRoot -Force

# Matching Newtonsoft.Json.dll supplied by the Archipelago NuGet package
Copy-Item -Path $nSoftPath -Destination $stagePatchersRoot -Force

Write-Host "Copying BepInEx core dependency files"

# MonoMod dependencies
Copy-Item -Path $mMBPPath -Destination $stageCoreRoot -Force
Copy-Item -Path $mMILHPath -Destination $stageCoreRoot -Force

Write-Host "Checking for optional additional BepInEx files"

# Anything placed under ref\extra\BepInEx is copied while preserving
# its BepInEx-relative folder structure
if (Test-Path $extraBepInExRoot) {
    $extraFiles = Get-ChildItem -Path $extraBepInExRoot -Force

    if ($extraFiles) {
        Write-Host "Optional BepInEx files found, adding them to the package"

        $extraFiles | Copy-Item -Destination $stageBepInExRoot -Recurse -Force
    }
    else {
        Write-Host "No optional BepInEx files found"
    }
}
else {
    Write-Host "No optional BepInEx folder found"
}

Write-Host "Thunderstore staging files copied"

Write-Host "Validating Thunderstore staging structure"

$stagedRequiredFiles = @(
    (Join-Path $stagingRoot "manifest.json")
    (Join-Path $stagingRoot "README.md")
    (Join-Path $stagingRoot "CHANGELOG.md")
    (Join-Path $stagingRoot "icon.png")
    (Join-Path $stagingRoot "LICENSE")

    (Join-Path $stagePluginsRoot "WOLAP.dll")
    (Join-Path $stagePluginsRoot "Archipelago.MultiClient.Net.dll")

    (Join-Path $stagePatchersRoot "WOLAP.DependencyPatcher.dll")
    (Join-Path $stagePatchersRoot "Newtonsoft.Json.dll")

    (Join-Path $stageCoreRoot "MonoMod.Backports.dll")
    (Join-Path $stageCoreRoot "MonoMod.ILHelpers.dll")
)

foreach ($stagedFile in $stagedRequiredFiles) {
    if (-not (Test-Path $stagedFile)) {
        throw "Required staged file was not found at $stagedFile"
    }
}

Write-Host "Thunderstore staging structure successfully validated"

Write-Host "Creating final Thunderstore .Zip package"

# Reads the staged manifest so the final .Zip name matches the package name and version
$stagedManifestPath = Join-Path $stagingRoot "manifest.json"
$manifestData = Get-Content -Path $stagedManifestPath -Raw | ConvertFrom-Json

$packageName = $manifestData.name
$packageVersion = $manifestData.version_number

# Creates the final .Zip filename from the manifest information
$finalZipName = "$packageName-$packageVersion.zip"
$finalZipPath = Join-Path $outputRoot $finalZipName

# If this exact version was already packaged, remove the old copy before creating the new one
if (Test-Path $finalZipPath) {
    Write-Host "$finalZipName already exists, replacing old package"
    Remove-Item -Path $finalZipPath -Force
}

# Compress the CONTENTS of staging, not the staging folder itself
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $finalZipPath -CompressionLevel Optimal

# Make sure the final package was actually created
if (-not (Test-Path $finalZipPath)) {
    throw "Final Thunderstore package was not created at $finalZipPath"
}

Write-Host "Thunderstore package successfully created at:"
Write-Host $finalZipPath