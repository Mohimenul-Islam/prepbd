FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PrepBD.Api/PrepBD.Api.csproj PrepBD.Api/
RUN dotnet restore PrepBD.Api/PrepBD.Api.csproj

COPY PrepBD.Api/ PrepBD.Api/
RUN dotnet publish PrepBD.Api/PrepBD.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./

# QuestionService reads Data/Questions from ContentRootPath at startup, so the
# bank must exist next to the DLL even if SDK content globbing changes.
COPY --from=build /src/PrepBD.Api/Data ./Data

# Heroku assigns $PORT at boot; shell-form CMD so it expands at runtime.
CMD ASPNETCORE_URLS=http://*:${PORT:-8080} dotnet PrepBD.Api.dll
