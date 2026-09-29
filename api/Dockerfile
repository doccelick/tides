FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Tides.Api/Tides.Api.csproj src/Tides.Api/
RUN dotnet restore src/Tides.Api/Tides.Api.csproj

COPY src/Tides.Api/ src/Tides.Api/
RUN dotnet publish src/Tides.Api/Tides.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tides.Api.dll"]