FROM --platform=$BUILDPLATFORM node:22-alpine AS ui
WORKDIR /ui
COPY --from=ui-src package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY --from=ui-src angular.json tsconfig.json tsconfig.app.json ./
COPY --from=ui-src src ./src
COPY --from=ui-src public ./public
RUN npx ng build --configuration production

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS api
ARG TARGETARCH
WORKDIR /src
COPY AnimalDatabase.Api.csproj ./
RUN dotnet restore -a $TARGETARCH
COPY . .
RUN dotnet publish AnimalDatabase.Api.csproj -c Release -a $TARGETARCH --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=1
COPY --from=api /out ./
COPY --from=ui /ui/dist/AnimalDatabaseProject/browser ./wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AnimalDatabase.Api.dll"]
