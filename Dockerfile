FROM mcr.microsoft.com/dotnet/runtime:8.0.8 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.100 AS build

ARG BUILD_CONFIGURATION=Release
ARG BUILD_NUMBER

RUN dotnet tool install --tool-path /tools dotnet-trace
RUN dotnet tool install --tool-path /tools dotnet-counters
RUN dotnet tool install --tool-path /tools dotnet-dump

WORKDIR "/src/"
COPY . .
RUN dotnet build "chainflip-lp/chainflip-lp.csproj" /p:BUILD_NUMBER=$BUILD_NUMBER -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "chainflip-lp/chainflip-lp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /tools
COPY --from=build /tools .

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "chainflip-lp.dll"]
