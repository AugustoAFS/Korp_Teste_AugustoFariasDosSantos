namespace Gateway.Exceptions;

public static class Errors
{
    public static readonly Error InvalidCredentials = new(
        "invalid_credentials",
        "Credenciais inválidas.",
        StatusCodes.Status401Unauthorized,
        "E-mail ou senha incorretos.");

    public static readonly Error InvalidSession = new(
        "invalid_session",
        "Sessão inválida ou expirada.",
        StatusCodes.Status401Unauthorized,
        "Faça login novamente para continuar.");

    public static readonly Error UserLocked = new(
        "user_locked",
        "Usuário temporariamente bloqueado.",
        StatusCodes.Status403Forbidden,
        "Tentativas de acesso excedidas. Tente novamente mais tarde.");

    public static readonly Error UserInactive = new(
        "user_inactive",
        "Usuário inativo.",
        StatusCodes.Status403Forbidden,
        "Este usuário não está habilitado a acessar o sistema.");

    public static readonly Error Forbidden = new(
        "forbidden",
        "Acesso negado.",
        StatusCodes.Status403Forbidden,
        "Seu perfil não permite acessar este recurso.");

    public static readonly Error ValidationFailed = new(
        "validation_error",
        "Requisição inválida.",
        StatusCodes.Status400BadRequest,
        "Um ou mais campos informados não passaram na validação.");

    public static readonly Error UserNotFound = new(
        "user_not_found",
        "Usuário não encontrado.",
        StatusCodes.Status404NotFound,
        "Não existe usuário com o identificador informado.");

    public static readonly Error EmailInUse = new(
        "email_in_use",
        "E-mail já cadastrado.",
        StatusCodes.Status409Conflict,
        "Já existe um usuário com este e-mail.");

    public static readonly Error RoleNotFound = new(
        "role_not_found",
        "Role inexistente.",
        StatusCodes.Status422UnprocessableEntity,
        "Uma ou mais roles informadas não existem.");

    public static readonly Error InvalidAntiforgeryToken = new(
        "invalid_antiforgery_token",
        "Token antifalsificação ausente ou inválido.",
        StatusCodes.Status400BadRequest,
        "Recarregue a página e tente novamente.");

    public static readonly Error TooManyRequests = new(
        "too_many_requests",
        "Muitas requisições.",
        StatusCodes.Status429TooManyRequests,
        "Aguarde alguns instantes antes de tentar novamente.");

    public static readonly Error InternalError = new(
        "internal_error",
        "Erro interno.",
        StatusCodes.Status500InternalServerError,
        "Ocorreu um erro inesperado ao processar a requisição.");
}
