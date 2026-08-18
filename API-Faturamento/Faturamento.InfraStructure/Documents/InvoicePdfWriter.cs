using System.Globalization;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Faturamento.InfraStructure.Documents;

public sealed class InvoicePdfWriter : IInvoicePdfWriter
{
    private const string Azul = "#1E40AF";
    private const string Laranja = "#C2410C";
    private const string Tinta = "#101828";
    private const string Fraco = "#6B7A90";
    private const string Linha = "#DCE3EC";
    private const string Fundo = "#F4F6F8";

    private static readonly CultureInfo Brasil = new("pt-BR");

    public byte[] Write(Invoice invoice)
        => Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(1.4f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(Tinta).FontFamily(Fonts.Calibri));

                pagina.Header().Element(cabecalho => Cabecalho(cabecalho, invoice));
                pagina.Content().Element(conteudo => Conteudo(conteudo, invoice));
                pagina.Footer().Element(Rodape);
            });
        }).GeneratePdf();

    private static void Cabecalho(IContainer container, Invoice invoice)
        => container.PaddingBottom(18).Column(coluna =>
        {
            coluna.Item().Row(linha =>
            {
                linha.ConstantItem(34).Height(34).Element(Marca);

                linha.RelativeItem().PaddingLeft(10).Column(texto =>
                {
                    texto.Item().Text("EMISSOR NF").FontSize(15).Bold().FontColor(Azul).LetterSpacing(0.08f);
                    texto.Item().Text("Documento auxiliar de nota fiscal").FontSize(8.5f).FontColor(Fraco);
                });

                linha.ConstantItem(150).AlignRight().Column(numero =>
                {
                    numero.Item().AlignRight().Text("NOTA Nº").FontSize(7.5f).FontColor(Fraco).LetterSpacing(0.14f);
                    numero.Item().AlignRight().Text($"{invoice.Number:0000000}")
                        .FontSize(22).Bold().FontColor(Azul);
                });
            });

            coluna.Item().PaddingTop(12).Height(2).Background(Azul);
        });

    private static void Marca(IContainer container)
        => container.Row(linha =>
        {
            linha.RelativeItem().Background(Laranja);
            linha.ConstantItem(3);
            linha.RelativeItem().Background(Azul);
        });

    private static void Conteudo(IContainer container, Invoice invoice)
        => container.Column(coluna =>
        {
            coluna.Spacing(16);

            coluna.Item().Element(dados => Dados(dados, invoice));
            coluna.Item().Element(itens => Itens(itens, invoice));
            coluna.Item().Element(totais => Totais(totais, invoice));
        });

    private static void Dados(IContainer container, Invoice invoice)
        => container.Background(Fundo).Border(1).BorderColor(Linha).Padding(12).Row(linha =>
        {
            linha.RelativeItem().Element(campo => Campo(campo, "SITUAÇÃO", "Fechada"));
            linha.RelativeItem().Element(campo => Campo(campo, "EMITENTE", invoice.IssuedByUserName));
            linha.RelativeItem().Element(campo => Campo(
                campo,
                "EMISSÃO",
                invoice.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Brasil)));
            linha.RelativeItem().Element(campo => Campo(
                campo,
                "FECHAMENTO",
                invoice.ClosedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Brasil) ?? "—"));
        });

    private static void Campo(IContainer container, string rotulo, string valor)
        => container.Column(coluna =>
        {
            coluna.Item().Text(rotulo).FontSize(7).FontColor(Fraco).LetterSpacing(0.12f);
            coluna.Item().PaddingTop(2).Text(valor).FontSize(10).SemiBold();
        });

    private static void Itens(IContainer container, Invoice invoice)
        => container.Column(coluna =>
        {
            coluna.Item().PaddingBottom(6).Text("ITENS").FontSize(7.5f).FontColor(Fraco).LetterSpacing(0.14f);

            coluna.Item().Table(tabela =>
            {
                tabela.ColumnsDefinition(colunas =>
                {
                    colunas.ConstantColumn(30);
                    colunas.ConstantColumn(90);
                    colunas.RelativeColumn();
                    colunas.ConstantColumn(70);
                });

                tabela.Header(cabecalho =>
                {
                    cabecalho.Cell().Element(Titulo).Text("#");
                    cabecalho.Cell().Element(Titulo).Text("CÓDIGO");
                    cabecalho.Cell().Element(Titulo).Text("DESCRIÇÃO");
                    cabecalho.Cell().Element(Titulo).AlignRight().Text("QTD.");
                });

                var indice = 1;

                foreach (var item in invoice.Items)
                {
                    tabela.Cell().Element(Celula).Text($"{indice++}").FontColor(Fraco);
                    tabela.Cell().Element(Celula).Text(item.ProductCode).SemiBold();
                    tabela.Cell().Element(Celula).Text(item.ProductDescription);
                    tabela.Cell().Element(Celula).AlignRight().Text($"{item.Quantity}").SemiBold();
                }
            });
        });

    private static IContainer Titulo(IContainer container)
        => container
            .BorderBottom(1.5f)
            .BorderColor(Azul)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(estilo => estilo.FontSize(7.5f).FontColor(Fraco).LetterSpacing(0.1f));

    private static IContainer Celula(IContainer container)
        => container
            .BorderBottom(1)
            .BorderColor(Linha)
            .PaddingVertical(6)
            .PaddingHorizontal(4);

    private static void Totais(IContainer container, Invoice invoice)
        => container.AlignRight().Width(200).Background(Fundo).Border(1).BorderColor(Linha).Padding(10)
            .Column(coluna =>
            {
                coluna.Item().Row(linha =>
                {
                    linha.RelativeItem().Text("Itens distintos").FontSize(9).FontColor(Fraco);
                    linha.AutoItem().Text($"{invoice.Items.Count}").FontSize(9).SemiBold();
                });

                coluna.Item().PaddingTop(4).Row(linha =>
                {
                    linha.RelativeItem().Text("Unidades totais").FontSize(9).FontColor(Fraco);
                    linha.AutoItem().Text($"{invoice.Items.Sum(item => item.Quantity)}")
                        .FontSize(12).Bold().FontColor(Azul);
                });
            });

    private static void Rodape(IContainer container)
        => container.PaddingTop(10).BorderTop(1).BorderColor(Linha).PaddingTop(6).Row(linha =>
        {
            linha.RelativeItem().Text(texto =>
            {
                texto.Span("Documento gerado automaticamente pelo Emissor NF em ").FontSize(7.5f).FontColor(Fraco);
                texto.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", Brasil)).FontSize(7.5f).FontColor(Fraco);
                texto.Span(". Sem valor fiscal.").FontSize(7.5f).FontColor(Fraco);
            });

            linha.AutoItem().Text(texto =>
            {
                texto.CurrentPageNumber().FontSize(7.5f).FontColor(Fraco);
                texto.Span(" / ").FontSize(7.5f).FontColor(Fraco);
                texto.TotalPages().FontSize(7.5f).FontColor(Fraco);
            });
        });
}
