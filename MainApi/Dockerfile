# مرحله بیلد (ساخت و پابلیش پروژه)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# کپی csproj و رستور پکیج‌ها
COPY ["MainApi/MainApi.csproj", "MainApi/"]
RUN dotnet restore "MainApi/MainApi.csproj"

# کپی کل سورس و بیلد
COPY . .
WORKDIR "/src/MainApi"
RUN dotnet publish "MainApi.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=true /p:PublishSingleFile=true

# مرحله ران‌تایم (اجرای برنامه در محیط سبک)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# کپی خروجی پابلیش از مرحله بیلد
COPY --from=build /app/publish .

# ایجاد کاربر غیر روت برای امنیت
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# پورت‌ها
EXPOSE 8080

# Healthcheck برای مانیتورینگ سرویس
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MainApi.dll"]