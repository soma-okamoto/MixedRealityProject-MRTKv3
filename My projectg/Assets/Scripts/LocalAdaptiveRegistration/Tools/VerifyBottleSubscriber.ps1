$ErrorActionPreference = 'Stop'
$verificationProject = Join-Path $PSScriptRoot 'Verification/BottleSubscriberVerification.csproj'
$buildPath = Join-Path ([System.IO.Path]::GetTempPath()) ('MetaBottleVerification-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildPath | Out-Null
$objPath = (Join-Path $buildPath 'obj') + [System.IO.Path]::DirectorySeparatorChar
$binPath = (Join-Path $buildPath 'bin') + [System.IO.Path]::DirectorySeparatorChar
& dotnet run --project $verificationProject "-p:BaseIntermediateOutputPath=$objPath" "-p:BaseOutputPath=$binPath"
if ($LASTEXITCODE -ne 0) {
    throw 'Bottle subscriber verification failed.'
}
