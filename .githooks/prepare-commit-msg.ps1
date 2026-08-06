param(
    [Parameter(Mandatory = $true)][string]$MessageFile,
    [string]$Source
)

$ErrorActionPreference = 'Stop'

if ($Source -eq 'merge' -or $Source -eq 'commit' -or $Source -eq 'squash')
{
    exit 0
}

$root = git rev-parse --show-toplevel
if (-not $root) { exit 0 }

$csproj = Join-Path $root 'src\HardwareTempWidget.App\HardwareTempWidget.App.csproj'
if (-not (Test-Path -LiteralPath $csproj)) { exit 0 }

$content = [System.IO.File]::ReadAllText($csproj)
$match = [regex]::Match($content, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $match.Success) { exit 0 }

$major = [int]$match.Groups[1].Value
$minor = [int]$match.Groups[2].Value
$patch = [int]$match.Groups[3].Value

$message = ''
if (Test-Path -LiteralPath $MessageFile)
{
    $message = [System.IO.File]::ReadAllText($MessageFile)
}

if ($message -match 'BREAKING CHANGE' -or $message -match '!\s*:')
{
    $major++; $minor = 0; $patch = 0
}
elseif ($message -match '(?m)^\s*feat([(:]|\s)')
{
    $minor++; $patch = 0
}
else
{
    $patch++
}

$newVersion = "$major.$minor.$patch"
$updated = [regex]::Replace($content, '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>")
[System.IO.File]::WriteAllText($csproj, $updated)
git add -- $csproj

Write-Host "Version bumped to $newVersion"
exit 0
