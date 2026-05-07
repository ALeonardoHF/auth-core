# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln .
COPY AuthProject.Api/AuthProject.Api.csproj AuthProject.Api/
COPY AuthProject.Application/AuthProject.Application.csproj AuthProject.Application/
COPY AuthProject.Domain/AuthProject.Domain.csproj AuthProject.Domain/
COPY AuthProject.Infrastructure/AuthProject.Infrastructure.csproj AuthProject.Infrastructure/
COPY AuthProject.Persistence/AuthProject.Persistence.csproj AuthProject.Persistence/
COPY AuthProject.Tests/AuthProject.Tests.csproj AuthProject.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish AuthProject.Api/AuthProject.Api.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthProject.Api.dll"]
