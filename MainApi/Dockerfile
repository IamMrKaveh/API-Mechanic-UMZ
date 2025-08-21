FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MainApi/MainApi.csproj", "MainApi/"]
RUN dotnet restore "MainApi/MainApi.csproj"
COPY . .
WORKDIR "/src/MainApi"
RUN dotnet publish "MainApi.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=true /p:PublishSingleFile=true
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "MainApi.dll"]
