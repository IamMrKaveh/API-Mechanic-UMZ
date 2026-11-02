# Base images are pinned by digest (security audit P3-002). Tags are kept for
# readability and are verified against the digest on pull. Refresh the digest
# from the registry API (Docker-Content-Digest of the tag's manifest list)
# when upgrading the SDK/runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:9ef70f7d811c55d76d3aac7605dc13d10a31a0addde17651f264d0981eb842db AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy the whole filtered context (see .dockerignore) once: Presentation references
# Application, Domain, Infrastructure, SharedKernel and SharedContracts, so a
# single-project restore would fail.
COPY . .
RUN dotnet restore "Presentation/Presentation.csproj"
WORKDIR "/src/Presentation"
RUN dotnet publish "Presentation.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=true /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:a34617c69283e27df7d9a52adf205b5584a6b41f637efad70913b03ce4b565f2 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Presentation.dll"]
