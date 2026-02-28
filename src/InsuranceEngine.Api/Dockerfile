FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/InsuranceEngine.Api/InsuranceEngine.Api.csproj", "src/InsuranceEngine.Api/"]
RUN dotnet restore "src/InsuranceEngine.Api/InsuranceEngine.Api.csproj"
COPY . .
WORKDIR "/src/src/InsuranceEngine.Api"
RUN dotnet build "InsuranceEngine.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InsuranceEngine.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InsuranceEngine.Api.dll"]
