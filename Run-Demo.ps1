# BuildTestRunDemo-WebSocketUtils.ps1
param (
    [string]$SolutionPath = ".\WebSocketLib.sln",
    [string]$DemoProjectPath = ".\WebSocketUtils.Demo\WebSocketUtils.Demo.csproj",
    [string]$DockerComposeFile = ".\docker-compose.yml"
)

$ErrorActionPreference = "Stop"

function Ensure-DockerServices {
    param([string]$ComposeFile)

    Write-Host "=== Starting Docker services (Kafka + Redis) ===" -ForegroundColor Cyan
    docker-compose -f $ComposeFile up -d

    Write-Host "Waiting a few seconds for Kafka and Redis to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # Optional: Check containers status
    docker ps --filter "name=kafka" --filter "name=redis"
}

function Build-And-Test {
    param([string]$Solution)

    Write-Host "=== Cleaning solution ===" -ForegroundColor Cyan
    dotnet clean $Solution -c Release

    Write-Host "=== Restoring packages ===" -ForegroundColor Cyan
    dotnet restore $Solution

    Write-Host "=== Building solution ===" -ForegroundColor Cyan
    dotnet build $Solution -c Release

    Write-Host "=== Running tests ===" -ForegroundColor Cyan
    dotnet test $Solution -c Release --no-build --logger "console;verbosity=detailed"
}

function Run-Demo {
    param([string]$DemoProject)

    Write-Host "=== Running Demo Project ===" -ForegroundColor Cyan
    dotnet run --project $DemoProject -c Release
}

# Main script execution
Ensure-DockerServices -ComposeFile $DockerComposeFile
Build-And-Test -Solution $SolutionPath
Run-Demo -DemoProject $DemoProjectPath
