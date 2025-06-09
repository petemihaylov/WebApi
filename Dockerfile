# Step 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore dependencies
COPY . ./
RUN dotnet restore "AiApi/AiApi.csproj"

# Build and publish
RUN dotnet publish "AiApi/AiApi.csproj" -c Release -o /app/publish

# Step 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 👇 This ensures the app listens on port 80 inside the container
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "AiApi.dll"]
