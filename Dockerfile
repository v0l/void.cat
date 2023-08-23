FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

#install npm
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
RUN apt-get install -y nodejs

#run yarn install
COPY . .
RUN cd VoidCat/spa \
    && npx yarn \
    && npx yarn build
    
RUN rm -rf VoidCat/appsettings.*.json \
    && git config --global --add safe.directory /app \
    && dotnet publish -c Release -o out VoidCat/VoidCat.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
RUN apt update \
    && apt install -y --no-install-recommends ffmpeg  \
    && apt clean \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "VoidCat.dll"]