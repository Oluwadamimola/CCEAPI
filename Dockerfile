# Use the official .NET SDK image for building 

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 

WORKDIR /src 

# Copy csproj and restore dependencies COPY *.csproj ./ 

RUN dotnet restore # Copy everything else and build COPY . ./ 

RUN dotnet publish -c Release -o /app/publish 

# Build runtime image FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime 

WORKDIR /app 

# Install required dependencies for SkiaSharp 

RUN apt-get update && apt-get install -y \ 
libfontconfig1 \ 
libfreetype6 \ 
libharfbuzz0b \ 
libicu72 \ 
libx11-6 \ 
libxext6 \ 
libxrender1 \ 
libgl1-mesa-glx \ 
ca-certificates \ 
&& rm -rf /var/lib/apt/lists/*

# Copy published app 
COPY --from=build /app/publish . 
# Create cache directory for images 
RUN mkdir -p /app/cache 
# Expose port EXPOSE 8080 
# Set environment variables 
ENV ASPNETCORE_URLS=http://+:8080 
ENV ASPNETCORE_ENVIRONMENT=Production 

# Run the application ENTRYPOINT ["dotnet", "CCEAPI.dll"]
