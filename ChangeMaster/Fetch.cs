using Content.Client.Changelog;

namespace ChangeMaster;

public static partial class Program
{
    private static async Task<int> Fetch()
    {
        var env = WorkflowRuntime.Get();

        var page = 0;
        var lastClosedPrDate = env.ChangelogFile.GetLastClosedPrDate();
        var entries = new List<ChangelogManager.ChangelogEntry>();
        var lastId = env.ChangelogFile.GetEntries().MaxBy(e => e.Id)?.Id + 1 ?? 0;

        Console.WriteLine("Collecting PRs...");

        while (true)
        {
            page++;
            var response =
                await env.Github.FetchPullRequests(env.Repository, lastClosedPrDate, Settings.ChangelogOkayLabel, page);

            if (response.Items.Count == 0)
            {
                Console.WriteLine("That's all");
                goto AfterLoop;
            }

            foreach (var pullRequest in response.Items)
            {
                if (pullRequest.Closed is null)
                    continue;

                Console.WriteLine($"#{pullRequest.Number}");

                if (pullRequest.Closed > lastClosedPrDate)
                {
                    lastClosedPrDate = (DateTime) pullRequest.Closed;
                    env.ChangelogFile.SetLastClosedPrDate(lastClosedPrDate);
                }

                try
                {
                    var changelog = pullRequest.ParseChangelog();
                    changelog.Id = lastId;
                    lastId += 1;
                    entries.Add(changelog);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Parsing changelog error #{pullRequest.Number}:\n\t{e.Message}");
                }
            }
        }

        AfterLoop:

        Console.WriteLine($"Collected entries: {entries.Count}");
        env.ChangelogFile.AppendEntries(entries);
        env.ChangelogFile.Save();

        return 0;
    }
}
