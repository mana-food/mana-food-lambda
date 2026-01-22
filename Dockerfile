FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/ManaFood.AuthLambda/ManaFood.AuthLambda.csproj", "ManaFood.AuthLambda/"]
RUN dotnet restore "ManaFood.AuthLambda/ManaFood.AuthLambda.csproj"

COPY src/ManaFood.AuthLambda/ ManaFood.AuthLambda/
WORKDIR "/src/ManaFood.AuthLambda"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ManaFood.AuthLambda.dll"]