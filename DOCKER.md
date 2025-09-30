# Docker Setup & Deployment Guide

## Prerequisites

- Docker Desktop installed and running
- .NET 9.0 SDK
- Python 3.8+ with venv

## Local Development with Docker

### Build the Docker image

```bash
# Build the image
docker build -t portfolioanalyzer-api:latest .

# This will:
# 1. Restore .NET dependencies
# 2. Build the API project
# 3. Install Python + dependencies
# 4. Create production-ready image
```

### Run with Docker Compose (Recommended)

```bash
# Start the API
docker-compose up

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop the API
docker-compose down
```

The API will be available at: `http://localhost:5000`

### Run Docker directly

```bash
# Run the container
docker run -d \
  --name portfolioanalyzer-api \
  -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  portfolioanalyzer-api:latest

# View logs
docker logs -f portfolioanalyzer-api

# Stop container
docker stop portfolioanalyzer-api
docker rm portfolioanalyzer-api
```

## Testing the API

```bash
# Health check
curl http://localhost:5000/api/portfolio/health

# Get stock price
curl http://localhost:5000/api/portfolio/price/AAPL

# Get portfolio summary
curl http://localhost:5000/api/portfolio/summary

# Get all assets
curl http://localhost:5000/api/portfolio/assets
```

## Troubleshooting

### Docker daemon not running
```bash
# macOS - Start Docker Desktop application
open -a Docker

# Verify Docker is running
docker --version
docker ps
```

### Python dependencies fail
```bash
# Rebuild without cache
docker build --no-cache -t portfolioanalyzer-api:latest .
```

### Port already in use
```bash
# Find process using port 5000
lsof -i :5000

# Kill the process
kill -9 <PID>

# Or use different port
docker run -p 5001:8080 portfolioanalyzer-api:latest
```

## Docker Image Details

**Base Images:**
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`
- Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`

**Python Setup:**
- Python 3.x installed via apt
- Virtual environment at `/opt/venv`
- Dependencies: `yfinance`, `pandas`

**Exposed Port:** 8080 (mapped to 5000 on host)

**Working Directory:** `/app`

## Next Steps

Once Docker works locally, proceed to:
1. GitHub Actions CI/CD setup
2. Azure Container Registry push
3. Azure App Service deployment
