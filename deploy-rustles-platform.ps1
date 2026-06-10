param(
    [string]$ServerIp = "158.174.211.41",
    [string]$User = "ubuntu",
    [string]$KeyPath = "$HOME\.ssh\rustles_bootstrap_key",
    [string]$ProjectPath = "C:\Users\Bartek\source\repos\rustles_solution\rustles_platform\rustles_platform\rustles_platform.csproj",
    [string]$RemoteAppRoot = "/opt/rustles/current"
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $ProjectPath
$publishDirectory = Join-Path $projectDirectory "bin\Release\net10.0\publish"
$zipPath = Join-Path $env:TEMP "rustles-deploy.zip"
$remoteTarget = "$User@$ServerIp"

Write-Host "Publishing Rustles..." -ForegroundColor Cyan
dotnet publish $ProjectPath -c Release

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "Packing publish output..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -Force

Write-Host "Uploading deploy bundle to $remoteTarget..." -ForegroundColor Cyan
scp -i $KeyPath $zipPath "${remoteTarget}:/tmp/rustles-deploy.zip"

$remoteScript = @'
set -euo pipefail

sudo systemctl stop rustles || true
sudo mkdir -p __REMOTE_APP_ROOT__
sudo mkdir -p __REMOTE_APP_ROOT__/Data

if [ -f __REMOTE_APP_ROOT__/Data/rustles-users.db ]; then
  sudo cp __REMOTE_APP_ROOT__/Data/rustles-users.db /tmp/rustles-users.db
fi

sudo find __REMOTE_APP_ROOT__ -mindepth 1 -maxdepth 1 ! -name Data -exec rm -rf {} +
sudo unzip -o /tmp/rustles-deploy.zip -d __REMOTE_APP_ROOT__ >/dev/null
sudo mkdir -p __REMOTE_APP_ROOT__/Data

if [ -f /tmp/rustles-users.db ]; then
  sudo cp /tmp/rustles-users.db __REMOTE_APP_ROOT__/Data/rustles-users.db
fi

sudo chown -R rustles:rustles __REMOTE_APP_ROOT__
sudo find __REMOTE_APP_ROOT__ -type d -exec chmod 755 {} \;
sudo find __REMOTE_APP_ROOT__ -type f -exec chmod 644 {} \;
sudo find __REMOTE_APP_ROOT__ -type f \( -name "*.so" -o -name "rustles_platform" \) -exec chmod 755 {} \;

sudo systemctl start rustles
sudo systemctl status rustles --no-pager
'@

$remoteScript = $remoteScript.Replace("__REMOTE_APP_ROOT__", $RemoteAppRoot)

Write-Host "Deploying on remote host..." -ForegroundColor Cyan
$remoteScript | ssh -i $KeyPath $remoteTarget "bash -s"

Write-Host "Deployment complete." -ForegroundColor Green
