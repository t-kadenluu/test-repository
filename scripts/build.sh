#!/bin/bash

# Enterprise Application Build Script

set -e  # Exit on any error

echo "=== Enterprise Application Build Started ==="

# Configuration
SOLUTION_FILE="EnterpriseApp.sln"
BUILD_CONFIGURATION="Release"
TEST_RESULTS_DIR="TestResults"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Clean previous build artifacts
log_info "Cleaning previous build artifacts..."
dotnet clean $SOLUTION_FILE --configuration $BUILD_CONFIGURATION
rm -rf $TEST_RESULTS_DIR

# Restore NuGet packages
log_info "Restoring NuGet packages..."
dotnet restore $SOLUTION_FILE

# Build the solution
log_info "Building solution..."
dotnet build $SOLUTION_FILE --configuration $BUILD_CONFIGURATION --no-restore

# Run unit tests
log_info "Running unit tests..."
dotnet test tests/UnitTests/ \
    --configuration $BUILD_CONFIGURATION \
    --no-build \
    --verbosity normal \
    --logger trx \
    --results-directory $TEST_RESULTS_DIR \
    --collect:"XPlat Code Coverage"

# Run integration tests
log_info "Running integration tests..."
dotnet test tests/IntegrationTests/ \
    --configuration $BUILD_CONFIGURATION \
    --no-build \
    --verbosity normal \
    --logger trx \
    --results-directory $TEST_RESULTS_DIR

# Build Docker images
log_info "Building Docker images..."
docker build -f deployment/docker/UserManagement.Dockerfile -t enterprise-app/user-management:latest .
docker build -f deployment/docker/OrderProcessing.Dockerfile -t enterprise-app/order-processing:latest .
docker build -f deployment/docker/PaymentGateway.Dockerfile -t enterprise-app/payment-gateway:latest .

# Package applications
log_info "Publishing applications..."
dotnet publish src/Services/UserManagement/UserManagement.API.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output ./publish/UserManagement \
    --no-build

dotnet publish src/Services/OrderProcessing/OrderProcessing.API.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output ./publish/OrderProcessing \
    --no-build

dotnet publish src/Services/PaymentGateway/PaymentGateway.API.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output ./publish/PaymentGateway \
    --no-build

# Generate code coverage report
if command -v reportgenerator &> /dev/null; then
    log_info "Generating code coverage report..."
    reportgenerator \
        "-reports:$TEST_RESULTS_DIR/**/coverage.cobertura.xml" \
        "-targetdir:$TEST_RESULTS_DIR/CodeCoverage" \
        "-reporttypes:Html"
else
    log_warning "ReportGenerator not found. Skipping code coverage report generation."
fi

log_info "=== Build completed successfully! ==="
log_info "Build artifacts available in ./publish/"
log_info "Test results available in ./$TEST_RESULTS_DIR/"

if [ -d "$TEST_RESULTS_DIR/CodeCoverage" ]; then
    log_info "Code coverage report available in ./$TEST_RESULTS_DIR/CodeCoverage/"
fi
