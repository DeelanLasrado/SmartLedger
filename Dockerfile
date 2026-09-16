# SmartLedger API — multi-stage container build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartLedger.sln ./
COPY SmartLedger.Domain/SmartLedger.Domain.csproj SmartLedger.Domain/
COPY SmartLedger.Application/SmartLedger.Application.csproj SmartLedger.Application/
COPY SmartLedger.Infrastructure/SmartLedger.Infrastructure.csproj SmartLedger.Infrastructure/
COPY SmartLedger.AI/SmartLedger.AI.csproj SmartLedger.AI/
COPY SmartLedger.API/SmartLedger.API.csproj SmartLedger.API/
COPY SmartLedger.Tests/SmartLedger.Tests.csproj SmartLedger.Tests/

RUN dotnet restore SmartLedger.sln

COPY . .
WORKDIR /src/SmartLedger.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV UseInMemoryDatabase=true
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartLedger.API.dll"]
