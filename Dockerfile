FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore "src/NutriCasa.Api/NutriCasa.Api.csproj"
RUN dotnet publish "src/NutriCasa.Api/NutriCasa.Api.csproj" -c Release -o out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NutriCasa.Api.dll"]
