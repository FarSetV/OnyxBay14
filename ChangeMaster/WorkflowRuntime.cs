using System.Text.Json;

namespace ChangeMaster;

public sealed class WorkflowRuntime
{
    private WorkflowRuntime()
    {
        Repository = GetEnvOrThrow("GITHUB_REPOSITORY");
        Token = GetEnvOrThrow("TOKEN");
        Event = GetEnv("GITHUB_EVENT_PATH");
        Github = new Github(Token);
        ChangelogFile = new ChangelogFile();

        if (Event is not null)
        {
            EventPayload =
                JsonSerializer.Deserialize<GithubModels.Event>(File.ReadAllText(Event), Settings.JsonOptions) ??
                throw new InvalidOperationException("Can't parse Github's Event Payload.");
        }
    }

    public string Repository { get; }
    public string Token { get; }
    public string? Event { get; }

    public Github Github { get; }

    public ChangelogFile ChangelogFile { get; }

    public GithubModels.Event? EventPayload { get; }

    public static WorkflowRuntime Get()
    {
        return new WorkflowRuntime();
    }

    private string GetEnvOrThrow(string name)
    {
        return GetEnv(name) ?? throw new InvalidOperationException(
            $"Environment variable not defined '{name}'");
    }

    private string? GetEnv(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
