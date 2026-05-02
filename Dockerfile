# base image (build-env is stage name)
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# where we work from inside docker container
WORKDIR /app

EXPOSE 8080

# copy sln and .csproj files into app directory(defined above)
# and then run 'dotnet restore' inside container which will
# get all packages it needs for our app
COPY "Prepora.sln" "Prepora.sln"
COPY "API/API.csproj" "API/API.csproj"
COPY "Application/Application.csproj" "Application/Application.csproj"
COPY "Persistence/Persistence.csproj" "Persistence/Persistence.csproj"
COPY "Domain/Domain.csproj" "Domain/Domain.csproj"
COPY "Infrastructure/Infrastructure.csproj" "Infrastructure/Infrastructure.csproj"

RUN dotnet restore "Prepora.sln"

# copy everything else and build ->
COPY . .
WORKDIR /app
# -> create build version of dotnet app
RUN dotnet publish -c Release -o out

# build a runtime image
# we don't want full sdk, we want the smaller version
# with just the runtime
# we needed sdk to have access to commands above (restore and publish)
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# copy PUBLISHED version of app to /app directory
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "API.dll" ]