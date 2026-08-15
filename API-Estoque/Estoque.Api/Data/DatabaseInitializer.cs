using Estoque.InfraStructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Api.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private const int DelayInSeconds = 3;
    private const int DatabaseDoesNotExist = 4060;
    private const int AuthenticationFailure = 18456;

    public static async Task PrepareDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();

        await WaitForServer(context, app.Logger);
        await ApplyMigrations(context, app.Logger);
    }

    private static async Task WaitForServer(EstoqueDbContext context, ILogger logger)
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
            catch (Exception ex) when (ErrorNumber(ex) == DatabaseDoesNotExist)
            {
                logger.LogInformation("Servidor disponível, banco ainda não existe");
                return;
            }
            catch (Exception ex) when (ErrorNumber(ex) == AuthenticationFailure)
            {
                throw new InvalidOperationException(
                    "Credenciais recusadas pelo servidor de banco. Verifique ConnectionStrings:EstoqueDb.", ex);
            }
            catch (Exception ex) when (ErrorNumber(ex) is not null)
            {
                logger.LogWarning(
                    "Banco indisponível na tentativa {Tentativa} de {Total}: {Motivo}",
                    attempt, MaxAttempts, ex.Message);

                if (attempt == MaxAttempts)
                    throw new InvalidOperationException(
                        $"Sem conexão com o banco após {MaxAttempts} tentativas. " +
                        "Verifique ConnectionStrings:EstoqueDb e se o container estoque-db está no ar.", ex);

                await Task.Delay(TimeSpan.FromSeconds(DelayInSeconds));
            }
        }
    }

    private static async Task ApplyMigrations(EstoqueDbContext context, ILogger logger)
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

    private static int? ErrorNumber(Exception exception)
        => exception switch
        {
            SqlException sql => sql.Number,
            { InnerException: not null } => ErrorNumber(exception.InnerException),
            _ => null
        };
}
