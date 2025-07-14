# Build .NET App
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /source

# Copy project files and restore as distinct layers
COPY ./Freetool.sln ./
COPY ./src/Freetool.Domain/Freetool.Domain.fsproj ./src/Freetool.Domain/
COPY ./src/Freetool.Application/Freetool.Application.fsproj ./src/Freetool.Application/
COPY ./src/Freetool.Infrastructure/Freetool.Infrastructure.fsproj ./src/Freetool.Infrastructure/
COPY ./src/Freetool.Api/Freetool.Api.fsproj ./src/Freetool.Api/
RUN dotnet restore

# Copy and publish app
COPY . .
RUN dotnet publish src/Freetool.Api/Freetool.Api.fsproj -c release -o /app

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Freetool.Api.dll"]