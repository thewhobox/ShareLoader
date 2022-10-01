#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y p7zip-full
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ShareLoader/ShareLoader.csproj", "ShareLoader/"]
COPY ["ShareLoader.Share/ShareLoader.Share.csproj", "ShareLoader.Share/"]
RUN dotnet restore "ShareLoader/ShareLoader.csproj"
COPY . .
WORKDIR "/src/ShareLoader"
RUN dotnet build "ShareLoader.csproj" -c Release -o /app/build -r linux-x64

FROM build AS publish
RUN dotnet publish "ShareLoader.csproj" -c Release -o /app/publish -r linux-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./ShareLoader"]