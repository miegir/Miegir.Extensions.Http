using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Mocks;

internal class MockConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private readonly Dictionary<string, int> accessCounts = new(StringComparer.OrdinalIgnoreCase);

    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    public override void Load() => OnReload();

    public override bool TryGet(string key, out string? value)
    {
        if (!base.TryGet(key, out value))
        {
            return false;
        }

        accessCounts.TryGetValue(key, out var count);
        accessCounts[key] = count + 1;
        return true;
    }

    public int GetAccessCount(string key)
    {
        accessCounts.TryGetValue(key, out var count);
        return count;
    }
}
