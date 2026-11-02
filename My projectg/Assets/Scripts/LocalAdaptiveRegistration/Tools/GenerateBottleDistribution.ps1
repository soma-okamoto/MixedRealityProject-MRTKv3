$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$registrationRoot = Join-Path $projectRoot 'Assets/Scripts/LocalAdaptiveRegistration'
$generatorRoot = Join-Path $projectRoot 'Assets/ROS# Project Team/ROS#/Runtime/Libraries/MessageGeneration'
$generatorSources = Get-ChildItem -LiteralPath $generatorRoot -Filter '*.cs' |
    Select-Object -ExpandProperty FullName

if (-not ('RosSharp.RosBridgeClient.MessageGeneration.MessageAutoGen' -as [type])) {
    # Windows PowerShell's default compiler is too old for the bundled ROS# sources.
    # Compile them with the installed .NET SDK, targeting the PowerShell .NET Framework.
    $sdkLine = dotnet --list-sdks | Select-Object -Last 1
    if ($sdkLine -notmatch '^(\S+) \[(.+)\]$') {
        throw 'An installed .NET SDK is required to run the ROS# generator.'
    }
    $compilerPath = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn/bincore/csc.dll'
    $frameworkPath = [System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
    $buildPath = Join-Path ([System.IO.Path]::GetTempPath()) ('MetaBottleMessageGen-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $buildPath | Out-Null
    $assemblyPath = Join-Path $buildPath 'MetaBottleMessageGen.dll'
    $compilerArgs = @('/nologo', '/target:library', '/define:NETFRAMEWORK', "/out:$assemblyPath")
    foreach ($reference in @('mscorlib.dll', 'System.dll', 'System.Core.dll')) {
        $compilerArgs += '/reference:' + (Join-Path $frameworkPath $reference)
    }
    & dotnet $compilerPath @compilerArgs @generatorSources
    if ($LASTEXITCODE -ne 0) {
        throw 'ROS# message generator compilation failed.'
    }
    Add-Type -LiteralPath $assemblyPath
}

[RosSharp.RosBridgeClient.MessageGeneration.MessageAutoGen]::isRos2 = $false
$definitionPath = Join-Path $registrationRoot 'RosMessages/Definitions/place_estimation/msg/BottleDistribution.msg'
$outputPath = Join-Path $registrationRoot 'RosMessages/Generated'
$warnings = [RosSharp.RosBridgeClient.MessageGeneration.MessageAutoGen]::GenerateSingleMessage(
    $definitionPath, $outputPath, 'place_estimation', $true)
foreach ($warning in $warnings) {
    Write-Warning $warning
}
