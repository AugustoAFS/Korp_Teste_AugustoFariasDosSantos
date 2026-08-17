using Estoque.ApplicationService.Interfaces;
using Estoque.ApplicationService.Services;
using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Entities;
using Estoque.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Estoque.TestesUnitarios.Services;

public sealed class ProductServiceTests
{
    private readonly IProductRepository _produtos = Substitute.For<IProductRepository>();
    private readonly IStockMovementRepository _movimentos = Substitute.For<IStockMovementRepository>();
    private readonly IEstoqueEventPublisher _publisher = Substitute.For<IEstoqueEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _usuario.Id.Returns(7L);
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(true);
        _service = new ProductService(
            _produtos, _movimentos, _publisher, _unitOfWork, _usuario, NullLogger<ProductService>.Instance);
    }

    private static Product Produto(int saldo = 10) => new("PAR-M8", "Parafuso sextavado M8", saldo);

    #region GetProductById

    [Fact]
    public async Task GetProductById_devolve_product_not_found_quando_nao_existe()
    {
        _produtos.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var resultado = await _service.GetProductById(Guid.CreateVersion7(), default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task GetProductById_projeta_o_produto_encontrado()
    {
        var produto = Produto();
        _produtos.GetById(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _service.GetProductById(produto.Id, default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Code.ShouldBe("PAR-M8");
        resultado.Value.Balance.ShouldBe(10);
    }

    #endregion

    #region GetProducts

    [Fact]
    public async Task GetProducts_devolve_a_pagina_pedida_com_o_total_do_repositorio()
    {
        var filtro = new ProductFilterRequest { Page = 2, Size = 5 };
        _produtos.GetPaged(filtro, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Product>)[Produto()], 37));

        var resultado = await _service.GetProducts(filtro, default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Page.ShouldBe(2);
        resultado.Value.Size.ShouldBe(5);
        resultado.Value.Total.ShouldBe(37);
        resultado.Value.Items.Count.ShouldBe(1);
    }

    #endregion

    #region CreateProduct

    [Fact]
    public async Task CreateProduct_recusa_codigo_ja_cadastrado_antes_de_abrir_transacao()
    {
        _produtos.CodeInUse("PAR-M8", null, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _service.CreateProduct(
            new CreateProductRequest { Code = "PAR-M8", Description = "Parafuso", Balance = 5 }, default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("product_code_in_use");
        await _unitOfWork.DidNotReceive().Begin(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_grava_entrada_de_estoque_e_publica_o_evento()
    {
        _produtos.CodeInUse(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.CreateProduct(
            new CreateProductRequest { Code = "PAR-M8", Description = "Parafuso sextavado M8", Balance = 15 }, default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Balance.ShouldBe(15);
        await _produtos.Received(1).Add(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _movimentos.Received(1).AddRange(
            Arg.Is<IReadOnlyList<StockMovement>>(m => m.Count == 1 && m[0].Quantity == 15),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishProductCreated(
            Arg.Any<Guid>(), "PAR-M8", "Parafuso sextavado M8", true, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_devolve_201_created()
    {
        _produtos.CodeInUse(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.CreateProduct(
            new CreateProductRequest { Code = "PAR-M8", Description = "Parafuso", Balance = 1 }, default);

        resultado.Status.ShouldBe(System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_desfaz_a_transacao_quando_perde_a_corrida_pelo_codigo()
    {
        _produtos.CodeInUse(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.CreateProduct(
            new CreateProductRequest { Code = "PAR-M8", Description = "Parafuso", Balance = 1 }, default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("product_code_in_use");
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_desfaz_a_transacao_quando_a_publicacao_explode()
    {
        _produtos.CodeInUse(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _publisher.PublishProductCreated(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha ao serializar"));

        await Should.ThrowAsync<InvalidOperationException>(() => _service.CreateProduct(
            new CreateProductRequest { Code = "PAR-M8", Description = "Parafuso", Balance = 1 }, default));

        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateProduct

    [Fact]
    public async Task UpdateProduct_devolve_product_not_found_quando_nao_existe()
    {
        _produtos.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var resultado = await _service.UpdateProduct(
            Guid.CreateVersion7(), new UpdateProductRequest { Code = "A", Description = "B" }, default);

        resultado.Error!.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task UpdateProduct_recusa_codigo_de_outro_produto()
    {
        var produto = Produto();
        _produtos.GetById(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);
        _produtos.CodeInUse("OUTRO", produto.Id, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _service.UpdateProduct(
            produto.Id, new UpdateProductRequest { Code = "OUTRO", Description = "Descrição" }, default);

        resultado.Error!.Code.ShouldBe("product_code_in_use");
    }

    [Fact]
    public async Task UpdateProduct_aplica_a_alteracao_e_publica_o_evento()
    {
        var produto = Produto();
        _produtos.GetById(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);
        _produtos.CodeInUse(Arg.Any<string>(), produto.Id, Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _service.UpdateProduct(
            produto.Id,
            new UpdateProductRequest { Code = "PAR-M10", Description = "Parafuso M10", Active = false },
            default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Code.ShouldBe("PAR-M10");
        resultado.Value.Active.ShouldBeFalse();
        await _publisher.Received(1).PublishProductUpdated(
            produto.Id, "PAR-M10", "Parafuso M10", false, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteProduct

    [Fact]
    public async Task DeleteProduct_devolve_product_not_found_quando_nao_existe()
    {
        _produtos.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var resultado = await _service.DeleteProduct(Guid.CreateVersion7(), default);

        resultado.Error!.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task DeleteProduct_recusa_produto_com_saldo()
    {
        var produto = Produto(saldo: 3);
        _produtos.GetById(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _service.DeleteProduct(produto.Id, default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("product_with_balance");
        await _unitOfWork.DidNotReceive().Begin(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_inativa_publica_e_devolve_204()
    {
        var produto = Produto(saldo: 0);
        _produtos.GetById(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _service.DeleteProduct(produto.Id, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(System.Net.HttpStatusCode.NoContent);
        produto.Active.ShouldBeFalse();
        produto.DeletedAt.ShouldNotBeNull();
        await _publisher.Received(1).PublishProductUpdated(
            produto.Id, produto.Code, produto.Description, false, Arg.Any<CancellationToken>());
    }

    #endregion
}
