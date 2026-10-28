# ایمیج اجرایی میزبان. از ریشهٔ ریپو build می‌شود:
#   docker build -f Adviehchin.App/Dockerfile -t adviehchin-app .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY Adviehchin.Domain/ ./Adviehchin.Domain/
COPY Adviehchin.Application/ ./Adviehchin.Application/
COPY Adviehchin.Infrastructure/ ./Adviehchin.Infrastructure/
COPY Adviehchin.App/ ./Adviehchin.App/
RUN dotnet publish Adviehchin.App/Adviehchin.App.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Adviehchin.App.dll"]
