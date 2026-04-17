# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SmartBank.Domain/SmartBank.Domain.csproj", "SmartBank.Domain/"]
COPY ["SmartBank.Application/SmartBank.Application.csproj", "SmartBank.Application/"]
COPY ["SmartBank.Infrastructure/SmartBank.Infrastructure.csproj", "SmartBank.Infrastructure/"]
COPY ["SmartBank.API/SmartBank.API.csproj", "SmartBank.API/"]

RUN dotnet restore "SmartBank.API/SmartBank.API.csproj"

COPY . .
WORKDIR /src/SmartBank.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
USER root
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartBank.API.dll"]
