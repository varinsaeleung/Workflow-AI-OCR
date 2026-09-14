FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/KmOcr.Domain/KmOcr.Domain.csproj src/KmOcr.Domain/
COPY src/KmOcr.Application/KmOcr.Application.csproj src/KmOcr.Application/
COPY src/KmOcr.Infrastructure/KmOcr.Infrastructure.csproj src/KmOcr.Infrastructure/
COPY src/KmOcr.Worker/KmOcr.Worker.csproj src/KmOcr.Worker/
RUN dotnet restore src/KmOcr.Worker/KmOcr.Worker.csproj

COPY src src
RUN dotnet publish src/KmOcr.Worker/KmOcr.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip poppler-utils libgomp1 libgl1 libglib2.0-0 \
    && pip3 install --break-system-packages --no-cache-dir paddlepaddle paddleocr pdf2image \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --uid 10001 appuser \
    && mkdir -p /app/uploads \
    && chown -R appuser:appuser /app
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app
USER appuser
ENTRYPOINT ["dotnet", "KmOcr.Worker.dll"]
