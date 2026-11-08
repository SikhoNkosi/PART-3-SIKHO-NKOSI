# ---------- Stage 1: build the API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first, so the package restore step is cached
# and does not run again unless the packages change
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore RaceDay.API/RaceDay.API.csproj

# Now copy the rest of the source code and publish a release build
COPY RaceDay.API/ RaceDay.API/
RUN dotnet publish RaceDay.API/RaceDay.API.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: the small image that actually runs ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The app listens on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the non-root user that comes with the image, not as root
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]