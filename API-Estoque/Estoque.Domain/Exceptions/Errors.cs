using System.Net;

namespace Estoque.Domain.Exceptions;

public static class Errors
{
    public static readonly Error ProductNotFound = new()
    {
        Code = "product_not_found",
        Title = "Produto não encontrado.",
        Status = HttpStatusCode.NotFound,
        Detail = "Não existe produto com o identificador informado."
    };

    public static readonly Error CodeInUse = new()
    {
        Code = "product_code_in_use",
        Title = "Código já cadastrado.",
        Status = HttpStatusCode.Conflict,
        Detail = "Já existe um produto com este código."
    };

    public static readonly Error ProductInactive = new()
    {
        Code = "product_inactive",
        Title = "Produto inativo.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "Este produto não está habilitado para movimentação."
    };

    public static readonly Error ProductWithBalance = new()
    {
        Code = "product_with_balance",
        Title = "Produto com saldo em estoque.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "Zere o saldo do produto antes de excluí-lo."
    };

    public static readonly Error InsufficientBalance = new()
    {
        Code = "insufficient_balance",
        Title = "Saldo insuficiente.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "O produto não tem saldo suficiente para a quantidade solicitada."
    };

    public static readonly Error ValidationFailed = new()
    {
        Code = "validation_error",
        Title = "Requisição inválida.",
        Status = HttpStatusCode.BadRequest,
        Detail = "Um ou mais campos informados não passaram na validação."
    };

    public static readonly Error InvalidSession = new()
    {
        Code = "invalid_session",
        Title = "Sessão inválida ou expirada.",
        Status = HttpStatusCode.Unauthorized,
        Detail = "Autentique-se novamente para continuar."
    };

    public static readonly Error Forbidden = new()
    {
        Code = "forbidden",
        Title = "Acesso negado.",
        Status = HttpStatusCode.Forbidden,
        Detail = "Seu perfil não permite acessar este recurso."
    };

    public static readonly Error TooManyRequests = new()
    {
        Code = "too_many_requests",
        Title = "Muitas requisições.",
        Status = HttpStatusCode.TooManyRequests,
        Detail = "Aguarde alguns instantes antes de tentar novamente."
    };

    public static readonly Error InternalError = new()
    {
        Code = "internal_error",
        Title = "Erro interno.",
        Status = HttpStatusCode.InternalServerError,
        Detail = "Ocorreu um erro inesperado ao processar a requisição."
    };
}
