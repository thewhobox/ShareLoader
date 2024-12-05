#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y p7zip-full
EXPOSE 80

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ShareLoader/ShareLoader.csproj", "ShareLoader/"]
COPY ["ShareLoader.Share/ShareLoader.Share.csproj", "ShareLoader.Share/"]
RUN dotnet restore "ShareLoader/ShareLoader.csproj"
COPY . .
WORKDIR "/src/ShareLoader"
ARG TARGETPLATFORM
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
        RID=linux-arm ; \
        echo "this ist arm" ; \
    fi \
    && dotnet build "ShareLoader.csproj" -c Release -o /app/build -r $RID

FROM build AS publish
ARG TARGETPLATFORM
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
        RID=linux-arm ; \
        echo "this ist arm" ; \
    fi \
    && dotnet publish "ShareLoader.csproj" -c Release -o /app/publish -r $RID --self-contained false /p:PublishSingleFile=false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV DDL_APIKEY=
ENV OMDB_APIKEY=

ENTRYPOINT ["dotnet", "ShareLoader.dll"]
