FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props Directory.Packages.props MomoPayment.sln* ./
COPY src/BuildingBlocks/ ./src/BuildingBlocks/
COPY src/Services/Identity/ ./src/Services/Identity/

RUN dotnet restore src/Services/Identity/Identity.API/Identity.API.csproj
RUN dotnet publish src/Services/Identity/Identity.API/Identity.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "Identity.API.dll"]
