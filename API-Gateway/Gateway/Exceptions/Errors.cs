namespace Gateway.Exceptions;

public static class Errors
{
    public static readonly Error InvalidCredentials = new()
    {
        Code = "invalid_credentials",
        Title = "Credenciais inválidas.",
        Status = StatusCodes.Status401Unauthorized,
        Detail = "E-mail ou senha incorretos."
    };

    public static readonly Error InvalidSession = new()
    {
        Code = "invalid_session",
        Title = "Sessão inválida ou expirada.",
        Status = StatusCodes.Status401Unauthorized,
        Detail = "Faça login novamente para continuar."
    };

    public static readonly Error UserLocked = new()
    {
        Code = "user_locked",
        Title = "Usuário temporariamente bloqueado.",
        Status = StatusCodes.Status403Forbidden,
        Detail = "Tentativas de acesso excedidas. Tente novamente mais tarde."
    };

    public static readonly Error UserInactive = new()
    {
        Code = "user_inactive",
        Title = "Usuário inativo.",
        Status = StatusCodes.Status403Forbidden,
        Detail = "Este usuário não está habilitado a acessar o sistema."
    };

    public static readonly Error Forbidden = new()
    {
        Code = "forbidden",
        Title = "Acesso negado.",
        Status = StatusCodes.Status403Forbidden,
        Detail = "Seu perfil não permite acessar este recurso."
    };

    public static readonly Error ValidationFailed = new()
    {
        Code = "validation_error",
        Title = "Requisição inválida.",
        Status = StatusCodes.Status400BadRequest,
        Detail = "Um ou mais campos informados não passaram na validação."
    };

    public static readonly Error UserNotFound = new()
    {
        Code = "user_not_found",
        Title = "Usuário não encontrado.",
        Status = StatusCodes.Status404NotFound,
        Detail = "Não existe usuário com o identificador informado."
    };

    public static readonly Error EmailInUse = new()
    {
        Code = "email_in_use",
        Title = "E-mail já cadastrado.",
        Status = StatusCodes.Status409Conflict,
        Detail = "Já existe um usuário com este e-mail."
    };

    public static readonly Error RoleNotFound = new()
    {
        Code = "role_not_found",
        Title = "Role inexistente.",
        Status = StatusCodes.Status422UnprocessableEntity,
        Detail = "Uma ou mais roles informadas não existem."
    };

    public static readonly Error InvalidAntiforgeryToken = new()
    {
        Code = "invalid_antiforgery_token",
        Title = "Token antifalsificação ausente ou inválido.",
        Status = StatusCodes.Status400BadRequest,
        Detail = "Recarregue a página e tente novamente."
    };

    public static readonly Error TooManyRequests = new()
    {
        Code = "too_many_requests",
        Title = "Muitas requisições.",
        Status = StatusCodes.Status429TooManyRequests,
        Detail = "Aguarde alguns instantes antes de tentar novamente."
    };

    public static readonly Error InternalError = new()
    {
        Code = "internal_error",
        Title = "Erro interno.",
        Status = StatusCodes.Status500InternalServerError,
        Detail = "Ocorreu um erro inesperado ao processar a requisição."
    };
}
