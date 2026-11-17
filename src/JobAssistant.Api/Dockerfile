FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until the .csproj changes.
COPY JobAssistant.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The .NET images include a non-root "app" user; don't run as root.
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobAssistant.Api.dll"]
