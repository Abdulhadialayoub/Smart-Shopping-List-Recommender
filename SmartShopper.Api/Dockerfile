# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY SmartShopper.Api.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Install Playwright CLI and Browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN playwright install --with-deps chromium

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Install Playwright runtime dependencies
# Using the official Playwright dependency installation
RUN apt-get update && apt-get install -y \
  libnss3 \
  libnspr4 \
  libatk1.0-0 \
  libatk-bridge2.0-0 \
  libcups2 \
  libdrm2 \
  libdbus-1-3 \
  libxkbcommon0 \
  libxcomposite1 \
  libxdamage1 \
  libxfixes3 \
  libxrandr2 \
  libgbm1 \
  libasound2 \
  libatspi2.0-0 \
  libxshmfence1 \
  libglib2.0-0 \
  && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
# Copy Playwright browsers from publish stage to runtime user's cache
# We assume 'appuser' will be created with home /home/appuser
COPY --from=publish /root/.cache/ms-playwright /home/appuser/.cache/ms-playwright
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SmartShopper.Api.dll"]