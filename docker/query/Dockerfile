FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install OpenJDK 17 for Antlr4
RUN apt-get update && \
    apt-get install -y openjdk-17-jdk && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["src/Mocha.Query/Mocha.Query.csproj", "src/Mocha.Query/"]
COPY ["src/Mocha.Antlr4.Generated/Mocha.Antlr4.Generated.csproj", "src/Mocha.Antlr4.Generated/"]
COPY ["src/Mocha.Core/Mocha.Core.csproj", "src/Mocha.Core/"]
COPY ["src/Mocha.Protocol.Generated/Mocha.Protocol.Generated.csproj", "src/Mocha.Protocol.Generated/"]
COPY ["src/Mocha.Storage/Mocha.Storage.csproj", "src/Mocha.Storage/"]
RUN dotnet restore "src/Mocha.Query/Mocha.Query.csproj"
COPY . .
WORKDIR "/src/src/Mocha.Query"
RUN dotnet build "Mocha.Query.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mocha.Query.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mocha.Query.dll"]
