# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything from the current context into /app
COPY . .

# ✅ CRITICAL FIX: Move into the correct subfolder before running dotnet
WORKDIR /app/WonderlandBackend
RUN dotnet restore

# Build and publish
RUN dotnet publish -c Release -o /app/out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/out .

EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "WonderlandBackend.dll"]