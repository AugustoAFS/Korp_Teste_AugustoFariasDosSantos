# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia todos os .csproj antes do restore para aproveitar o cache de camada:
# alterar código-fonte não invalida a camada de restore.
COPY ["Faturamento.Api/Faturamento.Api.csproj", "Faturamento.Api/"]
COPY ["Faturamento.ApplicationService/Faturamento.ApplicationService.csproj", "Faturamento.ApplicationService/"]
COPY ["Faturamento.Domain/Faturamento.Domain.csproj", "Faturamento.Domain/"]
COPY ["Faturamento.EventListeners/Faturamento.EventListeners.csproj", "Faturamento.EventListeners/"]
COPY ["Faturamento.InfraStructure/Faturamento.InfraStructure.csproj", "Faturamento.InfraStructure/"]
RUN dotnet restore "./Faturamento.Api/Faturamento.Api.csproj"

COPY . .
WORKDIR "/src/Faturamento.Api"
RUN dotnet build "./Faturamento.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Faturamento.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Faturamento.Api.dll"]
