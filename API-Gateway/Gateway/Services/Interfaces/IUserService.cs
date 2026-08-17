using Gateway.Dtos;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;

namespace Gateway.Services.Interfaces;

public interface IUserService
{
    Task<Result<PagedResult<UserResponse>>> GetUsers(UserFilterRequest filter, CancellationToken ct);

    Task<Result<UserResponse>> ById(long userId, CancellationToken ct);

    Task<Result<UserResponse>> Create(CreateUserRequest request, CancellationToken ct);

    Task<Result> ReplaceRoles(long userId, AssignRolesRequest request, CancellationToken ct);
}
