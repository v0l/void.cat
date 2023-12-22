FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

#install npm
ENV NODE_MAJOR=20
RUN apt update && \
    apt install -y ca-certificates curl gnupg && \
    mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list && \
    apt update && \
    apt install nodejs -y

#run yarn install
COPY . .
RUN git config --global --add safe.directory /app \
    && cd VoidCat/spa \
    && npx yarn \
    && npx yarn build
    
RUN rm -rf VoidCat/appsettings.*.json \
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