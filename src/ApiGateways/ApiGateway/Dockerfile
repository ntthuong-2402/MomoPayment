FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props Directory.Packages.props MomoPayment.sln* ./
COPY src/BuildingBlocks/ ./src/BuildingBlocks/
COPY src/ApiGateways/ApiGateway/ ./src/ApiGateways/ApiGateway/

RUN dotnet restore src/ApiGateways/ApiGateway/ApiGateway.csproj
RUN dotnet publish src/ApiGateways/ApiGateway/ApiGateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
