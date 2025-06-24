# Стейдж для збірки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копіюємо все та збираємо
COPY . ./
RUN dotnet publish -c Release -o out

# Стейдж для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Копіюємо з попереднього стейджу
COPY --from=build /app/out ./

# Вказуємо порт, якщо потрібно (наприклад 5000)
EXPOSE 5000

# Запускаємо додаток
ENTRYPOINT ["dotnet", "TelegramInsuranceBot.dll"]
