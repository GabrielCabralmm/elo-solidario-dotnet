FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY EloSolidario.sln .
COPY src/EloSolidario.Api/EloSolidario.Api.csproj src/EloSolidario.Api/
COPY tests/EloSolidario.Tests/EloSolidario.Tests.csproj tests/EloSolidario.Tests/
RUN dotnet restore
COPY . .
RUN dotnet test -c Release --no-restore
RUN dotnet publish src/EloSolidario.Api/EloSolidario.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet EloSolidario.Api.dll"]
