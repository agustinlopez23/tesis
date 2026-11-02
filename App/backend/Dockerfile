# API image for Railway (build context: App/backend).
FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /source
COPY global.json Marketplace.sln ./
COPY src/ src/
RUN dotnet restore src/Marketplace.Api/Marketplace.Api.csproj
RUN dotnet publish src/Marketplace.Api/Marketplace.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app ./
# Non-root user shipped with the official .NET images.
USER $APP_UID
EXPOSE 8080
# Railway injects PORT; Program.cs listens on it when present, otherwise on 8080.
ENTRYPOINT ["dotnet", "Marketplace.Api.dll"]
