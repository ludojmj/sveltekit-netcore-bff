FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /Server

FROM node:lts AS frontend
WORKDIR /client
COPY client .
RUN npm install
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /Server
COPY Directory.Build.props .
COPY Server .
COPY --from=frontend /client/build wwwroot
RUN dotnet restore Server.csproj
RUN dotnet build Server.csproj -c Release --no-restore
RUN dotnet publish -c Release --no-build -o out

FROM base AS final
WORKDIR /Server
COPY --from=build Server/out .
ENTRYPOINT ["dotnet", "Server.dll"]
