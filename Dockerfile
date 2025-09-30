# Stage 1: Build the .NET application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY PortfolioAnalyzer.sln ./
COPY PortfolioAnalyzer.Core/*.csproj ./PortfolioAnalyzer.Core/
COPY PortfolioAnalyzer.Api/*.csproj ./PortfolioAnalyzer.Api/
COPY PortfolioAnalyzer.Console/*.csproj ./PortfolioAnalyzer.Console/
COPY PortfolioAnalyzer.Tests/*.csproj ./PortfolioAnalyzer.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the API project
WORKDIR /src/PortfolioAnalyzer.Api
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Setup Python environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Python and dependencies
RUN apt-get update && \
    apt-get install -y python3 python3-pip python3-venv && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create Python virtual environment
RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Copy Python requirements and install
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy published .NET app from build stage
COPY --from=build /app/publish .

# Copy Python scripts (needed for data fetching)
COPY PortfolioAnalyzer.Core/Data/FetchData.py ./PortfolioAnalyzer.Core/Data/

# Expose port (Azure will map this)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "PortfolioAnalyzer.Api.dll"]
