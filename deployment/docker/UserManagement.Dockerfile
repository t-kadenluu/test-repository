FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/Services/UserManagement/UserManagement.API.csproj", "src/Services/UserManagement/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/DataAccess/DataAccess.csproj", "src/Shared/DataAccess/"]
COPY ["src/Shared/Security/Security.csproj", "src/Shared/Security/"]
COPY ["src/Shared/Logging/Logging.csproj", "src/Shared/Logging/"]

# Restore dependencies
RUN dotnet restore "src/Services/UserManagement/UserManagement.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/UserManagement"
RUN dotnet build "UserManagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --system --group appuser
USER appuser

ENTRYPOINT ["dotnet", "UserManagement.API.dll"]
