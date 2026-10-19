FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy the whole filtered context (see .dockerignore) once: Presentation references
# Application, Domain, Infrastructure, SharedKernel and SharedContracts, so a
# single-project restore would fail.
COPY . .
RUN dotnet restore "Presentation/Presentation.csproj"
WORKDIR "/src/Presentation"
RUN dotnet publish "Presentation.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=true /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Presentation.dll"]
