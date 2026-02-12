FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ChurchSecurityScheduler.csproj", "./"]
RUN dotnet restore "ChurchSecurityScheduler.csproj"
COPY . .
RUN dotnet build "ChurchSecurityScheduler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChurchSecurityScheduler.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChurchSecurityScheduler.dll"]