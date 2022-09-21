#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#-buster-slim-arm32v7
# -r linux-arm

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y p7zip-full
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src/ShareLoader
COPY ["ShareLoader/ShareLoader.csproj", "."]
RUN dotnet restore "./ShareLoader/ShareLoader.csproj" -r linux-arm
COPY . .
WORKDIR /src/ShareLoader.Share
COPY ["ShareLoader.Share/ShareLoader.Share.csproj", "."]
RUN dotnet restore "./ShareLoader.Share/ShareLoader.Share.csproj" -r linux-arm
COPY . .
WORKDIR "/src/ShareLoader."
RUN dotnet build "ShareLoader.csproj" -c Release -o /app/build -r linux-arm

FROM build AS publish
RUN dotnet publish "ShareLoader/ShareLoader.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShareLoader.dll"]