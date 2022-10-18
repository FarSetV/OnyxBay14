namespace ChangeMaster;

public static partial class Program
{
    private static async Task<int> Check()
    {
        var env = WorkflowRuntime.Get();
        var pr = env.EventPayload!.PullRequest!;

        try
        {
            var changelog = pr.ParseChangelog();

            await env.Github.RemoveLabel(env.Repository, pr.Number, Settings.ChangelogNotOkayLabel);
            await env.Github.AddLabel(env.Repository, pr.Number, Settings.ChangelogOkayLabel);
        }
        catch (Exceptions.ChangelogNotFound)
        {
            await env.Github.RemoveLabel(env.Repository, pr.Number, Settings.ChangelogOkayLabel);
            await env.Github.RemoveLabel(env.Repository, pr.Number, Settings.ChangelogNotOkayLabel);

            return 0;
        }
        catch (Exception e)
        {
            await env.Github.RemoveLabel(env.Repository, pr.Number, Settings.ChangelogOkayLabel);
            await env.Github.AddLabel(env.Repository, pr.Number, Settings.ChangelogNotOkayLabel);

            throw;
        }

        return 0;
    }
}
