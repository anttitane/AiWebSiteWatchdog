FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Bind to port 8080 by default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first to optimize restore caching
COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AiWebSiteWatchDog.Domain/AiWebSiteWatchDog.Domain.csproj AiWebSiteWatchDog.Domain/
COPY AiWebSiteWatchDog.Application/AiWebSiteWatchDog.Application.csproj AiWebSiteWatchDog.Application/
COPY AiWebSiteWatchDog.Infrastructure/AiWebSiteWatchDog.Infrastructure.csproj AiWebSiteWatchDog.Infrastructure/
COPY AiWebSiteWatchDog.API/AiWebSiteWatchDog.API.csproj AiWebSiteWatchDog.API/

RUN dotnet restore AiWebSiteWatchDog.API/AiWebSiteWatchDog.API.csproj

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish AiWebSiteWatchDog.API/AiWebSiteWatchDog.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AiWebSiteWatchDog.API.dll"]
