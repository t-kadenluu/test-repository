@echo off
REM Enterprise Application Build Script for Windows

echo === Enterprise Application Build Started ===

REM Configuration
set SOLUTION_FILE=EnterpriseApp.sln
set BUILD_CONFIGURATION=Release
set TEST_RESULTS_DIR=TestResults

REM Clean previous build artifacts
echo [INFO] Cleaning previous build artifacts...
dotnet clean %SOLUTION_FILE% --configuration %BUILD_CONFIGURATION%
if exist %TEST_RESULTS_DIR% rmdir /s /q %TEST_RESULTS_DIR%

REM Restore NuGet packages
echo [INFO] Restoring NuGet packages...
dotnet restore %SOLUTION_FILE%
if %ERRORLEVEL% neq 0 (
    echo [ERROR] NuGet restore failed
    exit /b 1
)

REM Build the solution
echo [INFO] Building solution...
dotnet build %SOLUTION_FILE% --configuration %BUILD_CONFIGURATION% --no-restore
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed
    exit /b 1
)

REM Run unit tests
echo [INFO] Running unit tests...
dotnet test tests\UnitTests\ ^
    --configuration %BUILD_CONFIGURATION% ^
    --no-build ^
    --verbosity normal ^
    --logger trx ^
    --results-directory %TEST_RESULTS_DIR% ^
    --collect:"XPlat Code Coverage"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Unit tests failed
    exit /b 1
)

REM Run integration tests
echo [INFO] Running integration tests...
dotnet test tests\IntegrationTests\ ^
    --configuration %BUILD_CONFIGURATION% ^
    --no-build ^
    --verbosity normal ^
    --logger trx ^
    --results-directory %TEST_RESULTS_DIR%

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Integration tests failed
    exit /b 1
)

REM Build Docker images
echo [INFO] Building Docker images...
docker build -f deployment\docker\UserManagement.Dockerfile -t enterprise-app/user-management:latest .
docker build -f deployment\docker\OrderProcessing.Dockerfile -t enterprise-app/order-processing:latest .
docker build -f deployment\docker\PaymentGateway.Dockerfile -t enterprise-app/payment-gateway:latest .

REM Package applications
echo [INFO] Publishing applications...
dotnet publish src\Services\UserManagement\UserManagement.API.csproj ^
    --configuration %BUILD_CONFIGURATION% ^
    --output .\publish\UserManagement ^
    --no-build

dotnet publish src\Services\OrderProcessing\OrderProcessing.API.csproj ^
    --configuration %BUILD_CONFIGURATION% ^
    --output .\publish\OrderProcessing ^
    --no-build

dotnet publish src\Services\PaymentGateway\PaymentGateway.API.csproj ^
    --configuration %BUILD_CONFIGURATION% ^
    --output .\publish\PaymentGateway ^
    --no-build

echo [INFO] === Build completed successfully! ===
echo [INFO] Build artifacts available in .\publish\
echo [INFO] Test results available in .\%TEST_RESULTS_DIR%\
