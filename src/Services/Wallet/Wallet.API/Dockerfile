FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props Directory.Packages.props MomoPayment.sln* ./
COPY src/BuildingBlocks/ ./src/BuildingBlocks/
COPY src/Services/Wallet/ ./src/Services/Wallet/

RUN dotnet restore src/Services/Wallet/Wallet.API/Wallet.API.csproj
RUN dotnet publish src/Services/Wallet/Wallet.API/Wallet.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002 50052
ENTRYPOINT ["dotnet", "Wallet.API.dll"]
