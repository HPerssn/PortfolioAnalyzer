# Stage 1: Build the Vue frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend

# Copy package files
COPY frontend/package*.json ./
RUN npm ci

# Copy frontend source
COPY frontend/ ./

# Build frontend for production
RUN npm run build

# Stage 2: Build the .NET application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

# Copy solution and project files
COPY backend/PortfolioAnalyzer.sln ./
COPY backend/PortfolioAnalyzer.Core/*.csproj ./PortfolioAnalyzer.Core/
COPY backend/PortfolioAnalyzer.Api/*.csproj ./PortfolioAnalyzer.Api/
COPY backend/PortfolioAnalyzer.Console/*.csproj ./PortfolioAnalyzer.Console/
COPY backend/PortfolioAnalyzer.Tests/*.csproj ./PortfolioAnalyzer.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY backend/ .

# Build the API project
WORKDIR /src/PortfolioAnalyzer.Api
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime - Setup Python environment and serve both frontend + API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Python and dependencies
RUN apt-get update && \
    apt-get install -y python3 python3-pip python3-venv && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create Python virtual environment at /venv (where ReadData.cs expects it)
RUN python3 -m venv /venv
ENV PATH="/venv/bin:$PATH"

# Copy Python requirements and install
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy published .NET app from build stage
COPY --from=backend-build /app/publish .

# Copy Python scripts (needed for data fetching)
COPY backend/PortfolioAnalyzer.Core/Data/FetchData.py ./PortfolioAnalyzer.Core/Data/

# Copy built frontend from frontend-build stage
COPY --from=frontend-build /app/frontend/dist ./wwwroot

# Expose port (Azure will map this)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "PortfolioAnalyzer.Api.dll"]
