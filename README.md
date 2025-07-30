# Enterprise Microservices Application

A comprehensive .NET 8 enterprise application built with microservices architecture, featuring multiple services, shared libraries, and modern DevOps practices.

## 🏗️ Architecture Overview

This application demonstrates enterprise-grade software architecture with:
- **8 Microservices** handling different business domains
- **8 Shared Libraries** for common functionality
- **3 Web Applications** for different user interfaces
- **3 Background Services** for asynchronous processing
- Comprehensive testing strategy with unit, integration, and end-to-end tests
- Complete DevOps pipeline with Docker and Kubernetes deployment

## 🚀 Technology Stack

**Backend Technologies:**
- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Redis (Caching)
- RabbitMQ (Message Bus)
- MassTransit

**Frontend Technologies:**
- Blazor Server
- ASP.NET Core MVC
- Bootstrap 5
- jQuery

**DevOps & Infrastructure:**
- Docker & Docker Compose
- Kubernetes
- Helm Charts
- Azure DevOps Pipelines
- Terraform

**Testing:**
- xUnit
- Moq
- FluentAssertions
- Integration Testing
- Load Testing with NBomber

## 📁 Project Structure

```
├── src/
│   ├── Services/               # 8 Microservices
│   │   ├── UserManagement/     # User registration & authentication
│   │   ├── OrderProcessing/    # Order lifecycle management
│   │   ├── PaymentGateway/     # Payment processing
│   │   ├── NotificationService/# Email, SMS, push notifications
│   │   ├── InventoryManagement/# Stock and inventory tracking
│   │   ├── ReportingService/   # Business reports & analytics
│   │   ├── ApiGateway/         # Central API entry point
│   │   └── AuditingService/    # System activity logging
│   │
│   ├── Shared/                 # 8 Shared Libraries
│   │   ├── Common/             # Common models & utilities
│   │   ├── DataAccess/         # EF Core contexts & patterns
│   │   ├── Messaging/          # Message bus implementation
│   │   ├── Security/           # JWT auth & security utilities
│   │   ├── Logging/            # Centralized logging (Serilog)
│   │   ├── Configuration/      # Configuration management
│   │   ├── Validation/         # Request & business validation
│   │   └── Caching/            # Redis caching implementation
│   │
│   ├── WebApps/                # 3 Web Applications
│   │   ├── AdminPortal/        # Administrative interface
│   │   ├── CustomerPortal/     # Customer-facing application
│   │   └── PublicWebsite/      # Marketing & information site
│   │
│   └── BackgroundServices/     # 3 Background Services
│       ├── DataSyncService/    # Data synchronization
│       ├── EmailProcessor/     # Email queue processing
│       └── ReportGenerator/    # Scheduled report generation
│
├── tests/                      # Comprehensive Testing
│   ├── UnitTests/              # Unit tests for all services
│   ├── IntegrationTests/       # API integration tests
│   ├── EndToEndTests/          # Full workflow tests
│   ├── LoadTests/              # Performance testing
│   └── TestUtilities/          # Test helpers & fixtures
│
├── deployment/                 # DevOps & Infrastructure
│   ├── docker/                 # Docker configurations
│   ├── kubernetes/             # K8s manifests
│   ├── helm/                   # Helm charts
│   ├── terraform/              # Infrastructure as Code
│   ├── azure-pipelines/        # CI/CD pipelines
│   └── environments/           # Environment-specific configs
│
├── docs/                       # Documentation
├── scripts/                    # Build & utility scripts
└── tools/                      # Development tools
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code

### Quick Start

1. **Clone and Setup**
   ```bash
   git clone <repository-url>
   cd test-repository
   dotnet restore EnterpriseApp.sln
   ```

2. **Start Infrastructure Services**
   ```bash
   docker-compose -f deployment/docker/docker-compose.infrastructure.yml up -d
   ```

3. **Run Services**
   ```bash
   # Windows
   scripts\build.bat
   
   # Linux/macOS
   ./scripts/build.sh
   ```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📄 License

This project is licensed under the MIT License.