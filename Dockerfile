# Use the .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj .
RUN dotnet restore

# Copy the rest of the application files
COPY . .
RUN dotnet publish -c Release -o out

# Use the .NET 8.0 runtime for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "AuthService.dll"]
