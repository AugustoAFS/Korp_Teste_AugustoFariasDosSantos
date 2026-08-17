using System.Security.Claims;
using Gateway.Dtos;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;
using Gateway.Exceptions;
using Gateway.Models;
using Gateway.Models.Enums;
using Gateway.Repositories.Interfaces;
using Gateway.Security.Interfaces;
using Gateway.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gateway.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IArgon2idHasher hasher,
    IHttpContextAccessor accessor,
    ILogger<UserService> logger) : IUserService
{
    private const string UniqueViolation = "23505";

    public async Task<Result<PagedResult<UserResponse>>> GetUsers(UserFilterRequest filter, CancellationToken ct)
    {
        var (items, total) = await userRepository.GetPaged(filter, ct);

        return new PagedResult<UserResponse>
        {
            Items = [.. items.Select(user => new UserResponse(user, user.Roles.Select(link => link.Role.Name)))],
            Page = filter.Page,
            Size = filter.Size,
            Total = total
        };
    }

    public async Task<Result<UserResponse>> ById(long userId, CancellationToken ct)
    {
        if (!Own(userId) && !Administrator())
        {
            logger.LogWarning("Consulta recusada: usuário {UserId} fora do próprio cadastro", userId);
            return Errors.Forbidden;
        }

        var user = await userRepository.ById(userId, ct);

        if (user is null)
        {
            logger.LogWarning("Consulta recusada: usuário {UserId} inexistente", userId);
            return Errors.UserNotFound;
        }

        return new UserResponse(user, user.Roles.Select(link => link.Role.Name));
    }

    public async Task<Result<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var requested = Assignable(request.Roles);
        var roles = await roleRepository.ByNames(requested, ct);

        if (roles.Count != requested.Length)
        {
            logger.LogWarning("Criação de usuário recusada: perfis inexistentes em {Roles}", requested);
            return Errors.RoleNotFound;
        }

        if (await userRepository.EmailInUse(request.Email, ct))
        {
            logger.LogWarning("Criação de usuário recusada: e-mail já cadastrado");
            return Errors.EmailInUse;
        }

        var user = new User(request.Name, request.Email, hasher.Hash(request.Password));

        foreach (var role in roles)
            user.AssignRole(role.Id);

        await userRepository.Add(user, ct);

        try
        {
            await userRepository.SaveChanges(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            logger.LogWarning("Criação de usuário recusada: e-mail cadastrado em requisição concorrente");
            return Errors.EmailInUse;
        }

        logger.LogInformation("Usuário {UserId} criado com os perfis {Roles}", user.Id, requested);
        return Result<UserResponse>.Created(new UserResponse(user, roles.Select(role => role.Name)));
    }

    public async Task<Result> ReplaceRoles(long userId, AssignRolesRequest request, CancellationToken ct)
    {
        var user = await userRepository.ById(userId, ct);

        if (user is null)
        {
            logger.LogWarning("Troca de perfis recusada: usuário {UserId} inexistente", userId);
            return Errors.UserNotFound;
        }

        var requested = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var roles = await roleRepository.ByNames(requested, ct);

        if (roles.Count != requested.Length)
        {
            logger.LogWarning("Troca de perfis recusada: perfis inexistentes em {Roles}", requested);
            return Errors.RoleNotFound;
        }

        user.ReplaceRoles(roles.Select(role => role.Id).ToArray());

        await userRepository.SaveChanges(ct);

        logger.LogInformation("Perfis do usuário {UserId} atualizados para {Roles}", userId, requested);
        return Result.NoContent();
    }

    private string[] Assignable(string[] requested)
        => Administrator() && requested.Length > 0
            ? requested.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [nameof(DefaultRole.Funcionario)];

    private bool Administrator()
        => accessor.HttpContext?.User.IsInRole(nameof(DefaultRole.Administrador)) == true;

    private bool Own(long userId)
        => long.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var current)
           && current == userId;
}
