FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props Directory.Packages.props MomoPayment.sln* ./
COPY src/BuildingBlocks/ ./src/BuildingBlocks/
COPY src/Services/Transaction/ ./src/Services/Transaction/

RUN dotnet restore src/Services/Transaction/Transaction.API/Transaction.API.csproj
RUN dotnet publish src/Services/Transaction/Transaction.API/Transaction.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
ENTRYPOINT ["dotnet", "Transaction.API.dll"]
