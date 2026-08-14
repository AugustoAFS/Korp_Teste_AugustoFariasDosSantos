using Gateway.Dtos;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;

namespace Gateway.Services.Interfaces;

public interface IAuthService
{
    Task<Result> Login(LoginRequest request, CancellationToken ct);

    Task<Result> Logout();

    Result<SessionResponse> Session();

    Result<TokenResponse> Token();
}
