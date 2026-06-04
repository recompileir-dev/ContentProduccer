FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ContentProducer.sln ./
COPY src/ContentProducer.Worker/ContentProducer.Worker.csproj src/ContentProducer.Worker/
RUN dotnet restore ContentProducer.sln

COPY . .
RUN dotnet publish src/ContentProducer.Worker/ContentProducer.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends ca-certificates tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

USER app

ENTRYPOINT ["dotnet", "ContentProducer.Worker.dll"]
