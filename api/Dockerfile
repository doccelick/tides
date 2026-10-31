FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Tides.Api/Tides.Api.csproj src/Tides.Api/
RUN dotnet restore src/Tides.Api/Tides.Api.csproj

COPY src/Tides.Api/ src/Tides.Api/
RUN dotnet publish src/Tides.Api/Tides.Api.csproj -c Release -o /app/publish --no-restore

# Same version as dotnet-tools.json in the repository root, which is outside this build context.
FROM build AS bundle
RUN dotnet tool install --global dotnet-ef --version 10.0.14
RUN /root/.dotnet/tools/dotnet-ef migrations bundle --project src/Tides.Api/Tides.Api.csproj --configuration Release --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /app
COPY --from=bundle /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tides.Api.dll"]