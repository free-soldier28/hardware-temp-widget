param(
    [string]$RemoteName,
    [string]$RemoteUrl
)

$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel
if (-not $root) { exit 0 }

# Determine the local commit being pushed from pre-push's stdin.
# Line format: "<local ref> <local sha> <remote ref> <remote sha>"
$targetSha = $null
while ($null -ne ($line = [Console]::ReadLine()))
{
    $parts = $line -split '\s+'
    if ($parts.Count -ge 3 -and $parts[0] -like 'refs/heads/*')
    {
        $targetSha = $parts[1]
        break
    }
}

if (-not $targetSha -or $targetSha -match '^0+$') { exit 0 }

$csproj = Join-Path $root 'src\HardwareTempWidget.App\HardwareTempWidget.App.csproj'
if (-not (Test-Path -LiteralPath $csproj)) { exit 0 }

# Read the version from the commit being pushed, NOT the working tree, so the tag
# always matches the code it points to.
$content = git show "${targetSha}:src/HardwareTempWidget.App/HardwareTempWidget.App.csproj" 2>$null
if (-not $content) { exit 0 }
$match = $content | Select-String '<Version>(\d+)\.(\d+)\.(\d+)</Version>' | ForEach-Object { $_.Matches[0] }
if (-not $match) { exit 0 }

$version = "$($match.Groups[1].Value).$($match.Groups[2].Value).$($match.Groups[3].Value)"
$tag = "v$version"

# Skip if the tag already exists locally or on the remote.
if (git rev-parse -q --verify "refs/tags/$tag" 2>$null) { exit 0 }
if (git ls-remote --tags $RemoteName "refs/tags/$tag" 2>$null) { exit 0 }

git tag $tag $targetSha
if ($LASTEXITCODE -ne 0) { exit 0 }

git push $RemoteName "refs/tags/$tag"
Write-Host "Version tag $tag pushed to $RemoteName"
exit $LASTEXITCODE