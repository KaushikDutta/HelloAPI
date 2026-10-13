# DOCKERFILE: the recipe for building a container image of the API.
# WHY a container: it packages the app with everything it needs, so it runs the same on
# your laptop, in AKS, or anywhere. "Works on my machine" stops being a problem.

# ---- Stage 1: BUILD ----
# The SDK image is large (compiler and tools). We use it only to build, then throw it away.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first and restore packages.
# WHY: Docker caches each step. If code changes but packages do not, this step is reused,
# so rebuilds are much faster.
COPY HelloApi.csproj .
RUN dotnet restore

# Now copy the rest of the code and publish an optimized build into /app.
COPY . .
RUN dotnet publish -c Release -o /app

# ---- Stage 2: RUN ----
# The runtime image is small and has no compiler. Smaller image = faster start, less attack surface.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Listen on port 8080 inside the container (the Kubernetes files use the same port).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as a non-root user. SECURITY: if someone breaks into the app, they get fewer rights.
USER $APP_UID

# The command that starts the app when the container starts.
ENTRYPOINT ["dotnet", "HelloApi.dll"]
