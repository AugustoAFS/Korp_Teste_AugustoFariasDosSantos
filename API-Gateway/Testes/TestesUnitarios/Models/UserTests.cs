using Gateway.Models;
using Shouldly;

namespace Gateway.TestesUnitarios.Models;

public sealed class UserTests
{
    private static readonly TimeSpan Bloqueio = TimeSpan.FromMinutes(15);
    private const int MaximoDeTentativas = 5;

    private static User Novo() => new("Augusto", "augusto@korp.com.br", "$argon2id$hash");

    [Fact]
    public void Usuario_nasce_ativo_e_desbloqueado()
    {
        var usuario = Novo();

        usuario.Active.ShouldBeTrue();
        usuario.Locked.ShouldBeFalse();
        usuario.FailedAccessCount.ShouldBe(0);
        usuario.LockedUntil.ShouldBeNull();
    }

    [Fact]
    public void Usuario_nasce_marcando_quando_a_senha_foi_definida()
        => Novo().PasswordChangedAt.ShouldNotBeNull();

    #region Bloqueio por tentativas

    [Fact]
    public void Tentativa_invalida_incrementa_o_contador_sem_bloquear()
    {
        var usuario = Novo();

        usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);

        usuario.FailedAccessCount.ShouldBe(1);
        usuario.Locked.ShouldBeFalse();
    }

    [Fact]
    public void Bloqueio_so_dispara_ao_atingir_o_maximo()
    {
        var usuario = Novo();

        for (var tentativa = 1; tentativa < MaximoDeTentativas; tentativa++)
        {
            usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);
            usuario.Locked.ShouldBeFalse();
        }

        usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);

        usuario.Locked.ShouldBeTrue();
    }

    [Fact]
    public void Bloqueio_zera_o_contador_para_a_proxima_rodada()
    {
        var usuario = Novo();

        for (var tentativa = 0; tentativa < MaximoDeTentativas; tentativa++)
            usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);

        usuario.FailedAccessCount.ShouldBe(0);
    }

    [Fact]
    public void Bloqueio_expira_no_prazo_configurado()
    {
        var usuario = Novo();

        for (var tentativa = 0; tentativa < MaximoDeTentativas; tentativa++)
            usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);

        usuario.LockedUntil!.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        usuario.LockedUntil.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.Add(Bloqueio));
    }

    [Fact]
    public void Acesso_valido_limpa_contador_e_bloqueio()
    {
        var usuario = Novo();

        for (var tentativa = 0; tentativa < MaximoDeTentativas; tentativa++)
            usuario.RegisterInvalidAccess(MaximoDeTentativas, Bloqueio);

        usuario.RegisterValidAccess();

        usuario.Locked.ShouldBeFalse();
        usuario.LockedUntil.ShouldBeNull();
        usuario.FailedAccessCount.ShouldBe(0);
    }

    #endregion

    #region Perfis

    [Fact]
    public void AssignRole_acrescenta_o_vinculo()
    {
        var usuario = Novo();

        usuario.AssignRole(1);
        usuario.AssignRole(2);

        usuario.Roles.Select(link => link.RoleId).ShouldBe([1L, 2L], ignoreOrder: true);
    }

    [Fact]
    public void ReplaceRoles_remove_o_que_saiu_da_lista()
    {
        var usuario = Novo();
        usuario.AssignRole(1);
        usuario.AssignRole(2);

        usuario.ReplaceRoles([2]);

        usuario.Roles.Select(link => link.RoleId).ShouldBe([2L]);
    }

    [Fact]
    public void ReplaceRoles_acrescenta_o_que_entrou()
    {
        var usuario = Novo();
        usuario.AssignRole(1);

        usuario.ReplaceRoles([1, 3]);

        usuario.Roles.Select(link => link.RoleId).ShouldBe([1L, 3L], ignoreOrder: true);
    }

    [Fact]
    public void ReplaceRoles_nao_duplica_o_que_ja_existia()
    {
        var usuario = Novo();
        usuario.AssignRole(1);

        usuario.ReplaceRoles([1]);

        usuario.Roles.Count.ShouldBe(1);
    }

    [Fact]
    public void ReplaceRoles_com_lista_vazia_deixa_o_usuario_sem_perfil()
    {
        var usuario = Novo();
        usuario.AssignRole(1);

        usuario.ReplaceRoles([]);

        usuario.Roles.ShouldBeEmpty();
    }

    #endregion
}
