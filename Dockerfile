# Build the ASP.NET Core MVC frontend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.Mvc/RaceDay.Mvc.csproj RaceDay.Mvc/
RUN dotnet restore RaceDay.Mvc/RaceDay.Mvc.csproj
COPY RaceDay.Mvc/ RaceDay.Mvc/
WORKDIR /src/RaceDay.Mvc
RUN dotnet publish RaceDay.Mvc.csproj -c Release -o /app/publish /p:UseAppHost=false

# Run the published MVC application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
