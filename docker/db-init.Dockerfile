FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:9ef70f7d811c55d76d3aac7605dc13d10a31a0addde17651f264d0981eb842db

# One-shot database migration job for the local compose stack.
# The repo's canonical schema history lives in Infrastructure/Persistence/Migrations
# (the root migrate.sql is stale, see docs/07-operations/deployment.md), so the
# schema is applied with `dotnet ef database update`, not with a raw SQL script.
# Base image digest matches Presentation/Dockerfile (refresh both together).
WORKDIR /src
COPY . .
RUN dotnet restore "Presentation/Presentation.csproj"

ENTRYPOINT ["dotnet", "ef", "database", "update", \
            "--project", "Infrastructure", \
            "--startup-project", "Presentation", \
            "--connection", "$ConnectionStrings__DefaultConnection"]
