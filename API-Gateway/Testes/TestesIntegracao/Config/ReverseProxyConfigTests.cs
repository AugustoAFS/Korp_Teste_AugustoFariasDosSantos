using System.Net;
using System.Net.Http.Json;
using Gateway.Dtos.Request;
using Gateway.TestesIntegracao.Suporte;
using Shouldly;

namespace Gateway.TestesIntegracao.Config;

[Collection(AmbienteCollection.Nome)]
public sealed class ReverseProxyConfigTests(PostgresFixture banco) : IAsyncLifetime
{
    private const string Senha = "Senha@123";

    private DownstreamStub _downstream = null!;
    private GatewayApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        await banco.LimparUsuarios();

        _downstream = new DownstreamStub();
        await _downstream.Iniciar();

        _api = new GatewayApiFactory(banco, _downstream.Endereco);
    }

    public async Task DisposeAsync()
    {
        _api.Dispose();
        await _downstream.DisposeAsync();
    }

    private async Task<HttpClient> Autenticado(string email)
    {
        var cliente = _api.Cliente();

        await cliente.PostAsJsonAsync("/api/v1/users", new CreateUserRequest
        {
            Name = "Usuário", Email = email, Password = Senha, Roles = []
        });

        await cliente.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest { Email = email, Password = Senha });

        return cliente;
    }

    #region Autorização das rotas proxiadas

    [Theory]
    [InlineData("/api/v1/produtos")]
    [InlineData("/api/v1/produtos/123")]
    [InlineData("/api/v1/notas")]
    [InlineData("/api/v1/notas/123")]
    public async Task Rota_proxiada_e_anonima_devolve_401(string rota)
    {
        var resposta = await _api.Cliente().GetAsync(rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/produtos")]
    [InlineData("/api/v1/produtos/123")]
    [InlineData("/api/v1/notas")]
    [InlineData("/api/v1/notas/123")]
    public async Task Coleção_e_item_sao_rotas_distintas_e_ambas_alcancam_o_downstream(string rota)
    {
        var cliente = await Autenticado("proxy@korp.com.br");
        _downstream.ZerarContador();

        var resposta = await cliente.GetAsync(rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        _downstream.Chamadas.ShouldBe(1);
    }

    #endregion

    #region Encaminhamento

    [Fact]
    public async Task Gateway_repassa_o_caminho_completo_para_o_downstream()
    {
        var cliente = await Autenticado("proxy@korp.com.br");

        var resposta = await cliente.GetAsync("/api/v1/produtos?page=2&size=5");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("ok");
    }

    [Fact]
    public async Task Rota_desconhecida_nao_e_proxiada()
    {
        var cliente = await Autenticado("proxy@korp.com.br");
        _downstream.ZerarContador();

        await cliente.GetAsync("/api/v1/rota-que-nao-existe");

        _downstream.Chamadas.ShouldBe(0);
    }

    #endregion
}
