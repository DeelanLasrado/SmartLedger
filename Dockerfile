# SmartLedger API — multi-stage container build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartLedger.Domain/SmartLedger.Domain.csproj SmartLedger.Domain/
COPY SmartLedger.Application/SmartLedger.Application.csproj SmartLedger.Application/
COPY SmartLedger.Infrastructure/SmartLedger.Infrastructure.csproj SmartLedger.Infrastructure/
COPY SmartLedger.AI/SmartLedger.AI.csproj SmartLedger.AI/
COPY SmartLedger.API/SmartLedger.API.csproj SmartLedger.API/

RUN dotnet restore SmartLedger.API/SmartLedger.API.csproj

COPY SmartLedger.Domain/ SmartLedger.Domain/
COPY SmartLedger.Application/ SmartLedger.Application/
COPY SmartLedger.Infrastructure/ SmartLedger.Infrastructure/
COPY SmartLedger.AI/ SmartLedger.AI/
COPY SmartLedger.API/ SmartLedger.API/

WORKDIR /src/SmartLedger.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV UseInMemoryDatabase=true
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartLedger.API.dll"]
