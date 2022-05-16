FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["accumulator/accumulator.fsproj", "accumulator/"]
RUN dotnet restore "accumulator/accumulator.fsproj"
COPY . .
WORKDIR "/src/accumulator"
RUN dotnet build "accumulator.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "accumulator.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "accumulator.dll"]
