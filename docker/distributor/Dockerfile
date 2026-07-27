FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install OpenJDK 17 for Antlr4
RUN apt-get update && \
    apt-get install -y openjdk-17-jdk && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["Mocha.sln", "Directory.Build.props", "./"]
COPY src/ src/
COPY tests/ tests/
COPY proto/ proto/
RUN dotnet restore "Mocha.sln"
RUN dotnet build "src/Mocha.Protocol.Generated/Mocha.Protocol.Generated.csproj" -c $BUILD_CONFIGURATION
RUN dotnet build "Mocha.sln" -c $BUILD_CONFIGURATION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Mocha.Distributor/Mocha.Distributor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mocha.Distributor.dll"]
