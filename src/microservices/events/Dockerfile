FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

# Copy project file and restore dependencies
COPY EventsService.csproj ./
RUN dotnet restore

# Copy the source code and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Use a smaller runtime image for the final application
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy the published output from the builder
COPY --from=builder /app .

# Expose the application port
EXPOSE 8082

# Command to run the executable
ENTRYPOINT ["dotnet", "EventsService.dll"]
