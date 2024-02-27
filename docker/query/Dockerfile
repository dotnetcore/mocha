FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Mocha.Query/Mocha.Query.csproj", "src/Mocha.Query/"]
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
