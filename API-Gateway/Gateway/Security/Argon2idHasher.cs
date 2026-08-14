using System.Security.Cryptography;
using System.Text;
using Gateway.Security.Interfaces;
using Konscious.Security.Cryptography;

namespace Gateway.Security;

public sealed class Argon2idHasher(string pepper) : IArgon2idHasher
{
    private const string Algoritmo = "argon2id";
    private const int Versao = 19;
    private const int MemoriaEmKb = 65536;
    private const int Iteracoes = 3;
    private const int Paralelismo = 1;
    private const int TamanhoDoHash = 32;
    private const int TamanhoDoSal = 16;

    private static readonly byte[] SalFicticio = new byte[TamanhoDoSal];

    private readonly byte[] _pepper = Encoding.UTF8.GetBytes(pepper);

    public string Hash(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanhoDoSal);
        var hash = Derivar(password, sal, MemoriaEmKb, Iteracoes, Paralelismo, TamanhoDoHash);

        return $"${Algoritmo}$v={Versao}$m={MemoriaEmKb},t={Iteracoes},p={Paralelismo}" +
               $"${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string phc)
    {
        if (!TentarLer(phc, out var memoria, out var iteracoes, out var paralelismo, out var sal, out var esperado))
            return false;

        var calculado = Derivar(password, sal, memoria, iteracoes, paralelismo, esperado.Length);

        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    public void DummyVerify(string password)
        => Derivar(password, SalFicticio, MemoriaEmKb, Iteracoes, Paralelismo, TamanhoDoHash);

    private byte[] Derivar(string senha, byte[] sal, int memoria, int iteracoes, int paralelismo, int tamanho)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
        {
            Salt = sal,
            MemorySize = memoria,
            Iterations = iteracoes,
            DegreeOfParallelism = paralelismo,
            KnownSecret = _pepper
        };

        return argon2.GetBytes(tamanho);
    }

    private static bool TentarLer(
        string phc,
        out int memoria,
        out int iteracoes,
        out int paralelismo,
        out byte[] sal,
        out byte[] hash)
    {
        memoria = iteracoes = paralelismo = 0;
        sal = hash = [];

        var partes = phc.Split('$');
        if (partes.Length != 6 || partes[1] != Algoritmo) return false;

        foreach (var parametro in partes[3].Split(','))
        {
            var par = parametro.Split('=');
            if (par.Length != 2 || !int.TryParse(par[1], out var valor)) return false;

            switch (par[0])
            {
                case "m": memoria = valor; break;
                case "t": iteracoes = valor; break;
                case "p": paralelismo = valor; break;
                default: return false;
            }
        }

        if (memoria <= 0 || iteracoes <= 0 || paralelismo <= 0) return false;

        try
        {
            sal = Convert.FromBase64String(partes[4]);
            hash = Convert.FromBase64String(partes[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        return sal.Length >= 8 && hash.Length >= 16;
    }
}
