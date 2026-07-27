FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mocha.sln", "Directory.Build.props", "./"]
COPY ["src/Mocha.Distributor/Mocha.Distributor.csproj", "src/Mocha.Distributor/"]
COPY ["src/Mocha.Core/Mocha.Core.csproj", "src/Mocha.Core/"]
COPY ["src/Mocha.Protocol.Generated/Mocha.Protocol.Generated.csproj", "src/Mocha.Protocol.Generated/"]
COPY ["src/Mocha.Storage/Mocha.Storage.csproj", "src/Mocha.Storage/"]
RUN dotnet restore "Mocha.sln"
COPY src/ src/
COPY proto/ proto/
RUN dotnet build "Mocha.sln" -c $BUILD_CONFIGURATION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Mocha.Distributor/Mocha.Distributor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mocha.Distributor.dll"]
