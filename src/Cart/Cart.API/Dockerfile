# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

COPY ["src/Cart/Cart.API/Cart.API.csproj", "src/Cart/Cart.API/"]
COPY ["src/Cart/Cart.Application/Cart.Application.csproj", "src/Cart/Cart.Application/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "src/SharedLibrary/"]
COPY ["src/Cart/Cart.Domain/Cart.Domain.csproj", "src/Cart/Cart.Domain/"]
COPY ["src/Cart/Cart.Infrastructure/Cart.Infrastructure.csproj", "src/Cart/Cart.Infrastructure/"]
RUN dotnet restore "./src/Cart/Cart.API/Cart.API.csproj"
COPY . .
WORKDIR "/src/Cart/Cart.API"
RUN dotnet build "./Cart.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cart.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cart.API.dll"]