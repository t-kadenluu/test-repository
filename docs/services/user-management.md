# User Management Service

## Overview
The User Management Service is responsible for handling all user-related operations including:
- User registration and authentication
- User profile management
- User role and permission management
- Password management and security

## API Endpoints

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/auth/logout` - User logout

## Configuration

### Database
The service uses SQL Server for data persistence. Configure the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=server;Database=UserManagementDb;Trusted_Connection=true;"
  }
}
```

### JWT Configuration
Configure JWT settings for authentication:

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "UserManagement.API",
    "Audience": "UserManagement.API",
    "ExpirationInMinutes": 60
  }
}
```

## Dependencies
- ASP.NET Core 8.0
- Entity Framework Core
- Serilog for logging
- JWT Bearer authentication
- AutoMapper for object mapping
- FluentValidation for request validation

## Running the Service

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB for development)

### Development
```bash
dotnet run --project src/Services/UserManagement/UserManagement.API.csproj
```

### Testing
```bash
dotnet test tests/UnitTests/Services/UserManagement.Tests.csproj
```
