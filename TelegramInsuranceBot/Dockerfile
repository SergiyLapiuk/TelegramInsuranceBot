# ������ ��� �����
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# ������� ��� �� �������
COPY . ./
RUN dotnet publish -c Release -o out

# ������ ��� �������
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# ������� � ������������ �������
COPY --from=build /app/out ./

# ������� ����, ���� ������� (��������� 5000)
EXPOSE 5000

# ��������� �������
ENTRYPOINT ["dotnet", "TelegramInsuranceBot.dll"]
