using System.Net;

namespace Faturamento.Domain.Exceptions;

public static class Errors
{
    public static readonly Error InvoiceNotFound = new()
    {
        Code = "invoice_not_found",
        Title = "Nota fiscal não encontrada.",
        Status = HttpStatusCode.NotFound,
        Detail = "Não existe nota fiscal com o identificador informado."
    };

    public static readonly Error InvoiceNotEditable = new()
    {
        Code = "invoice_not_editable",
        Title = "Nota fiscal não editável.",
        Status = HttpStatusCode.Conflict,
        Detail = "Só é possível alterar itens de uma nota aberta que não esteja imprimindo."
    };

    public static readonly Error InvoiceAlreadyPrinting = new()
    {
        Code = "invoice_already_printing",
        Title = "Impressão já em andamento.",
        Status = HttpStatusCode.Conflict,
        Detail = "Esta nota já está sendo impressa. Aguarde a conclusão."
    };

    public static readonly Error InvoiceAlreadyClosed = new()
    {
        Code = "invoice_already_closed",
        Title = "Nota fiscal já impressa.",
        Status = HttpStatusCode.Conflict,
        Detail = "Esta nota já foi impressa e não pode ser alterada nem reimpressa."
    };

    public static readonly Error InvoiceEmpty = new()
    {
        Code = "invoice_empty",
        Title = "Nota fiscal sem itens.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "Adicione ao menos um produto antes de imprimir a nota."
    };

    public static readonly Error InvoiceItemDuplicated = new()
    {
        Code = "invoice_item_duplicated",
        Title = "Produto já incluído na nota.",
        Status = HttpStatusCode.Conflict,
        Detail = "Este produto já consta na nota. Altere a quantidade do item existente."
    };

    public static readonly Error InvoiceItemNotFound = new()
    {
        Code = "invoice_item_not_found",
        Title = "Item não encontrado.",
        Status = HttpStatusCode.NotFound,
        Detail = "Não existe item com o identificador informado nesta nota."
    };

    public static readonly Error ProductNotFound = new()
    {
        Code = "product_not_found",
        Title = "Produto não encontrado.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "Este produto ainda não está disponível no faturamento. Tente novamente em instantes."
    };

    public static readonly Error ProductInactive = new()
    {
        Code = "product_inactive",
        Title = "Produto inativo.",
        Status = HttpStatusCode.UnprocessableEntity,
        Detail = "Este produto não está habilitado para uso em notas fiscais."
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

    public static readonly Error InvoiceNotClosed = new()
    {
        Code = "invoice_not_closed",
        Title = "Nota ainda não fechada.",
        Status = HttpStatusCode.Conflict,
        Detail = "O PDF só fica disponível depois que a nota é fechada e o estoque é baixado."
    };

    public static readonly Error AiDisabled = new()
    {
        Code = "ai_disabled",
        Title = "Assistente indisponível.",
        Status = HttpStatusCode.ServiceUnavailable,
        Detail = "O assistente de IA não está configurado nesta instalação. Adicione os itens manualmente."
    };

    public static readonly Error AiUnavailable = new()
    {
        Code = "ai_unavailable",
        Title = "Assistente fora do ar.",
        Status = HttpStatusCode.ServiceUnavailable,
        Detail = "Não foi possível interpretar o pedido agora. Tente de novo ou adicione os itens manualmente."
    };

    public static readonly Error InternalError = new()
    {
        Code = "internal_error",
        Title = "Erro interno.",
        Status = HttpStatusCode.InternalServerError,
        Detail = "Ocorreu um erro inesperado ao processar a requisição."
    };
}
