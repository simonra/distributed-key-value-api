FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Make curl health checks in local compose possible. Add wget because it's nice to have.
RUN apt-get update && apt-get install -y curl && apt-get install -y wget
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "KeyValueApi.dll"]
