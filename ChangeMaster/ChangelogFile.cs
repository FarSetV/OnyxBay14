using Content.Client.Changelog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChangeMaster;

public sealed class ChangelogFile
{
    private readonly FileStructure _structure;

    public ChangelogFile()
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        var content = File.ReadAllText(Path);

        _structure = deserializer.Deserialize<FileStructure>(content);
    }

    public static string Path { get; } = "./Resources/Changelog/Changelog.yml";

    public void Save()
    {
        var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        var content = serializer.Serialize(_structure);

        File.WriteAllText(Path, content);
    }

    public DateTime GetLastClosedPrDate()
    {
        return _structure.LastChangelog;
    }

    public void SetLastClosedPrDate(DateTime date)
    {
        _structure.LastChangelog = date;
    }

    public List<ChangelogManager.ChangelogEntry> GetEntries()
    {
        return _structure.Entries;
    }

    public void AppendEntries(IEnumerable<ChangelogManager.ChangelogEntry> entries)
    {
        _structure.Entries = _structure.Entries.Concat(entries).ToList();
    }

    private sealed class FileStructure
    {
        public List<ChangelogManager.ChangelogEntry> Entries { get; set; } = new();
        public DateTime LastChangelog { get; set; } = DateTime.Now;
    }
}
