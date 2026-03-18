FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PlexWatch/PlexWatch.csproj PlexWatch/
RUN dotnet restore PlexWatch/PlexWatch.csproj
COPY . .
RUN dotnet publish PlexWatch/PlexWatch.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends gosu curl && rm -rf /var/lib/apt/lists/*

COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://*:80

HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost/health || exit 1

ENTRYPOINT ["./entrypoint.sh"]
