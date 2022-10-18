using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChangeMaster;

internal static class Settings
{
    public static readonly DirectoryInfo WorkspaceFolder = new(Path.GetFullPath("./", Directory.GetCurrentDirectory()));

    public static readonly DirectoryInfo ChangelogsCache =
        new(Path.GetFullPath("./Resources/Changelogger/Changelogger.yml", WorkspaceFolder.FullName));

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static readonly string ChangelogOkayLabel = "Чейнджлог: :white_check_mark:";
    public static readonly string ChangelogNotOkayLabel = "Чейнджлог: :x:";
}
