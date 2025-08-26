FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install dependencies
RUN apt-get update && apt-get install -y --no-install-recommends gosu curl && rm -rf /var/lib/apt/lists/*

# Copy default configuration
COPY DefaultConfiguration/ /app_defaults/_Configuration/

# Copy application and startup scripts
COPY . .

# Make entrypoint executable
RUN chmod +x entrypoint.sh

# Set default ASP.NET Core port
ENV ASPNETCORE_URLS=http://*:80

# Healthcheck
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 CMD curl --fail http://localhost/health || exit 1

# Entrypoint
ENTRYPOINT ["./entrypoint.sh"]
