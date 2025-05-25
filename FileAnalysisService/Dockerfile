# 1) Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# ─── установить curl для healthcheck ───────────────────────────────────────
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

EXPOSE 80

# 2) Build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["FileAnalysisService.csproj", "./"]
RUN dotnet restore "./FileAnalysisService.csproj"
COPY . .
RUN dotnet build "FileAnalysisService.csproj" -c Release -o /app/build

# 3) Publish
FROM build AS publish
RUN dotnet publish "FileAnalysisService.csproj" -c Release -o /app/publish

# 4) Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileAnalysisService.dll"]
