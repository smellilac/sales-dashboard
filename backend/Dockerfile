# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer: copy only the files that affect package restore first, so the
# Docker cache is reused when only source code changes.
COPY Directory.Build.props Directory.Packages.props .editorconfig SalesDashboard.slnx ./
COPY src/SalesDashboard.Api/SalesDashboard.Api.csproj src/SalesDashboard.Api/
COPY tests/SalesDashboard.Api.Tests/SalesDashboard.Api.Tests.csproj tests/SalesDashboard.Api.Tests/
RUN dotnet restore src/SalesDashboard.Api/SalesDashboard.Api.csproj

# Now copy the rest and publish.
COPY . .
RUN dotnet publish src/SalesDashboard.Api/SalesDashboard.Api.csproj \
    -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Run as the non-root user provided by the base image.
USER $APP_UID

# aspnet:10.0 defaults ASPNETCORE_HTTP_PORTS to 8080; make it explicit.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SalesDashboard.Api.dll"]
