# Usar la imagen oficial de .NET Core SDK como base
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copiar el csproj y restaurar las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar todo y construir la aplicación
COPY . ./
RUN dotnet publish -c Release -o out

# Usar la imagen oficial de .NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Instalar libwkhtmltox
RUN apt-get update && apt-get install -y libwkhtmltox

# Establecer el directorio de trabajo y copiar la aplicación construida
WORKDIR /app
COPY --from=build-env /app/out .

ENV APP_NET_CORE proyecto-inkamanu-net.dll

CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE
