using System.Security.Claims;
using Gateway.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Gateway.TestesUnitarios.Security;

public sealed class TokenServiceTests
{
    private const string Chave = "LjYrUfDqiP3ZYVLilk7D_V2BQs6s6larC-nhKVO0UNJNpJ6418pe_U3tqG1-4wrQ";
    private readonly TokenService _service = new(Chave);

    private static ClaimsPrincipal Usuario(params string[] perfis)
    {
        var identidade = new ClaimsIdentity("cookie", ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, "7"));
        identidade.AddClaim(new Claim(ClaimTypes.Name, "Augusto"));
        identidade.AddClaim(new Claim(ClaimTypes.Email, "augusto@korp.com.br"));

        foreach (var perfil in perfis)
            identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));

        return new ClaimsPrincipal(identidade);
    }

    private JsonWebToken Emitir(params string[] perfis)
        => new JsonWebTokenHandler().ReadJsonWebToken(_service.Issue(Usuario(perfis)));

    private static string Valor(JsonWebToken token, string tipo)
        => token.Claims.First(claim => claim.Type == tipo).Value;

    [Fact]
    public void Token_carrega_emissor_e_audiencia_combinados_com_os_servicos()
    {
        var token = Emitir();

        token.Issuer.ShouldBe(TokenService.Issuer);
        token.Audiences.ShouldContain(TokenService.Audience);
    }

    [Fact]
    public void Token_leva_o_identificador_do_usuario_em_sub()
        => Valor(Emitir(), JwtRegisteredClaimNames.Sub).ShouldBe("7");

    [Fact]
    public void Token_leva_nome_e_email()
    {
        var token = Emitir();

        Valor(token, JwtRegisteredClaimNames.Name).ShouldBe("Augusto");
        Valor(token, JwtRegisteredClaimNames.Email).ShouldBe("augusto@korp.com.br");
    }

    [Fact]
    public void Perfis_viram_claims_role_que_os_servicos_leem()
    {
        var token = Emitir("Administrador", "Gerente");

        var perfis = token.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value).ToArray();

        perfis.ShouldBe(["Administrador", "Gerente"], ignoreOrder: true);
    }

    [Fact]
    public void Usuario_sem_perfil_gera_token_sem_role()
        => Emitir().Claims.ShouldNotContain(claim => claim.Type == "role");

    [Fact]
    public void Token_e_de_vida_curta_porque_e_so_para_o_salto_interno()
    {
        var token = Emitir();

        (token.ValidTo - token.ValidFrom).TotalSeconds.ShouldBe(TokenService.SecondsToLive, tolerance: 2);
    }

    [Fact]
    public void Cada_emissao_tem_jti_proprio()
        => Valor(Emitir(), JwtRegisteredClaimNames.Jti)
            .ShouldNotBe(Valor(Emitir(), JwtRegisteredClaimNames.Jti));

    [Fact]
    public void Token_e_assinado_com_hmac_sha256()
        => Emitir().Alg.ShouldBe(SecurityAlgorithms.HmacSha256);

    [Fact]
    public void Principal_sem_claims_nao_explode_e_emite_token_com_campos_vazios()
    {
        var token = new JsonWebTokenHandler()
            .ReadJsonWebToken(_service.Issue(new ClaimsPrincipal(new ClaimsIdentity())));

        Valor(token, JwtRegisteredClaimNames.Sub).ShouldBeEmpty();
    }

    [Fact]
    public async Task Token_emitido_e_validado_pela_mesma_chave()
    {
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(
            _service.Issue(Usuario("Gerente")),
            new TokenValidationParameters
            {
                ValidIssuer = TokenService.Issuer,
                ValidAudience = TokenService.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Chave)),
                ClockSkew = TimeSpan.Zero
            });

        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Token_e_recusado_por_chave_diferente()
    {
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(
            _service.Issue(Usuario("Gerente")),
            new TokenValidationParameters
            {
                ValidIssuer = TokenService.Issuer,
                ValidAudience = TokenService.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes("outra-chave-de-64-caracteres-para-o-teste-de-assinatura-aqui-ok")),
                ClockSkew = TimeSpan.Zero
            });

        resultado.IsValid.ShouldBeFalse();
    }
}
