FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-kv-api
WORKDIR /App

# Copy everything
COPY KeyValueApi/ ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-health-checker
WORKDIR /App
COPY HealthChecker/ ./
RUN dotnet publish --configuration Release --no-self-contained -o out


# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-kv-api /App/out .
COPY --from=build-health-checker /App/out /HealthChecker/
HEALTHCHECK CMD [ "dotnet", "/HealthChecker/HealthChecker.dll", "--", "http://localhost:8080/healthz" ]
ENTRYPOINT ["dotnet", "KeyValueApi.dll"]
