namespace Gateway.Security.Interfaces;

public interface IArgon2idHasher
{
    string Hash(string password);

    bool Verify(string password, string phc);

    void DummyVerify(string password);
}
