using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Faturamento.InfraStructure.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private const int DelayInSeconds = 3;
    private const string DatabaseDoesNotExist = "3D000";
    private const string AuthenticationFailure = "28P01";

    public static async Task PrepareDatabase(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<FaturamentoDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);

        await WaitForServer(context, logger);
        await ApplyMigrations(context, logger);
    }

    private static async Task WaitForServer(FaturamentoDbContext context, ILogger logger)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();

                logger.LogInformation("Conexão com o banco estabelecida");
                return;
            }
            catch (Exception ex) when (SqlState(ex) == DatabaseDoesNotExist)
            {
                logger.LogInformation("Servidor disponível, banco ainda não existe");
                return;
            }
            catch (Exception ex) when (SqlState(ex) == AuthenticationFailure)
            {
                throw new InvalidOperationException(
                    "Credenciais recusadas pelo servidor de banco. Verifique ConnectionStrings:FaturamentoDb.", ex);
            }
            catch (Exception ex) when (ex is NpgsqlException or SocketException)
            {
                logger.LogWarning(
                    "Banco indisponível na tentativa {Tentativa} de {Total}: {Motivo}",
                    attempt, MaxAttempts, ex.Message);

                if (attempt == MaxAttempts)
                    throw new InvalidOperationException(
                        $"Sem conexão com o banco após {MaxAttempts} tentativas. " +
                        "Verifique ConnectionStrings:FaturamentoDb e se o container faturamento-db está no ar.", ex);

                await Task.Delay(TimeSpan.FromSeconds(DelayInSeconds));
            }
        }
    }

    private static async Task ApplyMigrations(FaturamentoDbContext context, ILogger logger)
    {
        if (!await context.Database.CanConnectAsync())
        {
            logger.LogInformation("Criando o banco e aplicando as migrations");

            await context.Database.MigrateAsync();

            logger.LogInformation("Banco criado e migrations aplicadas");
            return;
        }

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();

        if (pending.Length == 0)
        {
            logger.LogInformation("Banco em dia, nenhuma migration pendente");
            return;
        }

        logger.LogInformation(
            "Aplicando {Total} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        await context.Database.MigrateAsync();

        logger.LogInformation("Migrations aplicadas");
    }

    private static string? SqlState(Exception exception)
        => exception switch
        {
            PostgresException postgres => postgres.SqlState,
            { InnerException: not null } => SqlState(exception.InnerException),
            _ => null
        };
}
