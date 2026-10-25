FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props Directory.Packages.props MomoPayment.sln* ./
COPY src/BuildingBlocks/ ./src/BuildingBlocks/
COPY src/Services/Notification/ ./src/Services/Notification/

RUN dotnet restore src/Services/Notification/Notification.API/Notification.API.csproj
RUN dotnet publish src/Services/Notification/Notification.API/Notification.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "Notification.API.dll"]
