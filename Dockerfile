#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#-buster-slim-arm32v7

FROM mcr.microsoft.com/dotnet/aspnet:3.1-buster-slim-arm32v7 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y p7zip-full
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["ShareLoader.csproj", "."]
RUN dotnet restore "./ShareLoader.csproj" -r linux-arm
COPY . .
WORKDIR "/src/."
RUN dotnet build "ShareLoader.csproj" -c Release -o /app/build -r linux-arm

FROM build AS publish
RUN dotnet publish "ShareLoader.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShareLoader.dll"]