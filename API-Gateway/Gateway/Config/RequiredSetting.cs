namespace Gateway.Config;

public static class RequiredSetting
{
    public static string Of(IConfiguration configuration, string key, int minLength)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{Path(configuration, key)} não configurada.");

        if (value.Length < minLength)
            throw new InvalidOperationException($"{Path(configuration, key)} precisa de ao menos {minLength} caracteres.");

        return value;
    }

    private static string Path(IConfiguration configuration, string key)
        => configuration is IConfigurationSection section ? $"{section.Path}:{key}" : key;
}
