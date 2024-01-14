FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 4319

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Mocha.Distributor/Mocha.Distributor.csproj", "src/Mocha.Distributor/"]
COPY ["src/Mocha.Core/Mocha.Core.csproj", "src/Mocha.Core/"]
COPY ["src/Mocha.Protocol.Generated/Mocha.Protocol.Generated.csproj", "src/Mocha.Protocol.Generated/"]
COPY ["src/Mocha.Storage/Mocha.Storage.csproj", "src/Mocha.Storage/"]
RUN dotnet restore "src/Mocha.Distributor/Mocha.Distributor.csproj"
COPY . .
WORKDIR "/src/src/Mocha.Distributor"
RUN dotnet build "Mocha.Distributor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mocha.Distributor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mocha.Distributor.dll"]
