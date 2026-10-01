# syntax=docker/dockerfile:1

# ---- Build: restore, compile and run the tests ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first so `dotnet restore` is cached until dependencies change.
COPY TaskManagement.sln Directory.Build.props ./
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY src/TaskManagement.Api/TaskManagement.Api.csproj src/TaskManagement.Api/
COPY tests/TaskManagement.Tests/TaskManagement.Tests.csproj tests/TaskManagement.Tests/
RUN dotnet restore TaskManagement.sln

COPY src/ src/
COPY tests/ tests/

# The image is only produced if the test suite passes.
RUN dotnet test tests/TaskManagement.Tests/TaskManagement.Tests.csproj --no-restore -c Release

RUN dotnet publish src/TaskManagement.Api/TaskManagement.Api.csproj --no-restore -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime: ASP.NET Core only, running as a non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
