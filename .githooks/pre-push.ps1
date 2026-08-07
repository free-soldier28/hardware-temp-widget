param(
    [string]$RemoteName,
    [string]$RemoteUrl
)

$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel
if (-not $root) { exit 0 }

$csproj = Join-Path $root 'src\HardwareTempWidget.App\HardwareTempWidget.App.csproj'
if (-not (Test-Path -LiteralPath $csproj)) { exit 0 }

$content = [System.IO.File]::ReadAllText($csproj)
$match = [regex]::Match($content, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $match.Success) { exit 0 }

$tag = "v$($match.Groups[1].Value).$($match.Groups[2].Value).$($match.Groups[3].Value)"

# Determine the local commit being pushed from pre-push's stdin.
# Format per line: "<local ref> <local sha> <remote ref> <remote sha>"
$lines = @()
while (-not [Console]::In.EndOfStream)
{
    $lines += [Console]::In.ReadLine()
}

$targetSha = $null
foreach ($line in $lines)
{
    $parts = $line -split '\s+'
    if ($parts.Count -ge 2 -and $parts[0] -like 'refs/heads/*')
    {
        $targetSha = $parts[1]
        break
    }
}

if (-not $targetSha -or $targetSha -match '^0+$') { exit 0 }

# Skip if the tag already exists locally or on the remote.
$tagExists = git rev-parse -q --verify "refs/tags/$tag" 2>$null
if ($tagExists) { exit 0 }

$remoteTags = git ls-remote --tags $RemoteName "refs/tags/$tag" 2>$null
if ($remoteTags) { exit 0 }

git tag $tag $targetSha
if ($LASTEXITCODE -ne 0) { exit 0 }

git push $RemoteName "refs/tags/$tag"
Write-Host "Version tag $tag pushed to $RemoteName"
exit 0