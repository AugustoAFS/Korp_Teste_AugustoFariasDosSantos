using Gateway.Security;
using Shouldly;

namespace Gateway.TestesUnitarios.Security;

public sealed class Argon2idHasherTests
{
    private const string Pepper = "pepper-de-desenvolvimento-do-emissor";
    private readonly Argon2idHasher _hasher = new(Pepper);

    [Fact]
    public void Hash_produz_o_formato_phc_esperado()
    {
        var phc = _hasher.Hash("Senha@123");

        var partes = phc.Split('$');
        partes.Length.ShouldBe(6);
        partes[1].ShouldBe("argon2id");
        partes[2].ShouldBe("v=19");
        partes[3].ShouldBe("m=65536,t=3,p=1");
    }

    [Fact]
    public void Verify_aceita_a_senha_correta()
        => _hasher.Verify("Senha@123", _hasher.Hash("Senha@123")).ShouldBeTrue();

    [Fact]
    public void Verify_recusa_a_senha_errada()
        => _hasher.Verify("Senha@124", _hasher.Hash("Senha@123")).ShouldBeFalse();

    [Fact]
    public void Verify_diferencia_maiuscula_de_minuscula()
        => _hasher.Verify("senha@123", _hasher.Hash("Senha@123")).ShouldBeFalse();

    [Fact]
    public void Mesma_senha_gera_hashes_diferentes_por_causa_do_sal()
    {
        var primeiro = _hasher.Hash("Senha@123");
        var segundo = _hasher.Hash("Senha@123");

        primeiro.ShouldNotBe(segundo);
        _hasher.Verify("Senha@123", primeiro).ShouldBeTrue();
        _hasher.Verify("Senha@123", segundo).ShouldBeTrue();
    }

    #region Pepper

    [Fact]
    public void Hash_gerado_com_outro_pepper_nao_e_aceito()
    {
        var phc = new Argon2idHasher("outro-pepper-completamente-diferente").Hash("Senha@123");

        _hasher.Verify("Senha@123", phc).ShouldBeFalse();
    }

    [Fact]
    public void Trocar_o_pepper_invalida_toda_senha_ja_armazenada()
    {
        var phc = _hasher.Hash("Senha@123");

        new Argon2idHasher("pepper-novo").Verify("Senha@123", phc).ShouldBeFalse();
    }

    #endregion

    #region Entrada malformada

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-phc")]
    [InlineData("$bcrypt$v=19$m=65536,t=3,p=1$c2Fs$aGFzaA==")]
    [InlineData("$argon2id$v=19$m=0,t=3,p=1$c2Fsc2Fsc2Fsc2Fs$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("$argon2id$v=19$m=65536,t=3,p=1$nao-e-base64$aGFzaA==")]
    [InlineData("$argon2id$v=19$m=65536,x=3,p=1$c2Fsc2Fsc2Fsc2Fs$aGFzaGhhc2hoYXNoaGFzaA==")]
    public void Verify_recusa_phc_malformado_sem_explodir(string phc)
        => _hasher.Verify("Senha@123", phc).ShouldBeFalse();

    [Fact]
    public void Verify_recusa_sal_curto_demais()
        => _hasher.Verify("Senha@123", "$argon2id$v=19$m=65536,t=3,p=1$c2Fs$aGFzaGhhc2hoYXNoaGFzaA==")
            .ShouldBeFalse();

    #endregion

    [Fact]
    public void DummyVerify_nao_explode_e_serve_para_igualar_o_tempo_de_resposta()
        => Should.NotThrow(() => _hasher.DummyVerify("Senha@123"));
}
