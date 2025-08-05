# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["backend/SmartCampusConnectBackend.csproj", "backend/"]
RUN dotnet restore "backend/SmartCampusConnectBackend.csproj"

# Copy the rest of the source code and build the application
COPY . .
WORKDIR "/src/backend"
RUN dotnet build "SmartCampusConnectBackend.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SmartCampusConnectBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 5000 (or the port your app listens on) and set the entrypoint
EXPOSE 5000
ENTRYPOINT ["dotnet", "SmartCampusConnectBackend.dll"]