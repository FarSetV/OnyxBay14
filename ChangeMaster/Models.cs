using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Content.Client.Changelog;

namespace ChangeMaster;

public static class GithubModels
{
    public sealed class PullRequest
    {
        private static readonly Regex BodyRegex = new(@"(:cl:|🆑)(.+)?\r\n((.|\n|\r)+?)\r\n\/(:cl:|🆑)",
            RegexOptions.Multiline);

        private static readonly Regex SplitRegex = new(@"(^\w+):\s*(.*)", RegexOptions.Multiline);

        [JsonPropertyName("html_url")] public string Url { get; init; } = string.Empty;

        public int Number { get; init; } = 0;
        public string Body { get; init; } = string.Empty;

        [JsonPropertyName("user")] public User Author { get; init; } = new();

        [JsonPropertyName("created_at")] public DateTime Opened { get; init; } = DateTime.Now;

        [JsonPropertyName("closed_at")] public DateTime? Closed { get; init; } = null;

        public Label[] Labels { get; init; } = Array.Empty<Label>();

        public ChangelogManager.ChangelogEntry ParseChangelog()
        {
            if (string.IsNullOrEmpty(Body))
                throw new Exceptions.ChangelogNotFound("🚫 Тело пулл реквеста пустое.");

            var changesBody = BodyRegex.Match(Body);

            if (!changesBody.Success)
                throw new Exceptions.ChangelogNotFound("🚫 Чейнджлог не обнаружен.");

            var matches = SplitRegex.Matches(changesBody.Value);

            if (matches.Count == 0)
                throw new Exceptions.ChangelogIsEmpty("🚫 Чейнджлог пустой или имеет неверный формат.");

            var author = changesBody.Groups[2].Value.Trim();

            if (string.IsNullOrEmpty(author))
                author = Author.Login;

            ChangelogManager.ChangelogEntry changelog = new()
            {
                Author = author,
                Time = Closed ?? DateTime.Now,
                Changes = new List<ChangelogManager.ChangelogChange>(),
                Id = 0
            };

            foreach (Match match in matches)
            {
                var parts = match.Value.Split(':');

                if (parts.Length < 2)
                    throw new InvalidOperationException($"🚫 Неверный формат изменения: '{match.Value}'");

                var prefix = parts[0].Trim();
                var message = string.Join(':', parts[1..]).Trim();

                changelog.Changes.Add(new ChangelogManager.ChangelogChange
                {
                    Type = Enum.Parse<ChangelogManager.ChangelogLineType>(prefix, true),
                    Message = message
                });
            }

            return changelog;
        }
    }

    public sealed class User
    {
        public string Login { get; init; } = string.Empty;
    }

    public sealed class Event
    {
        public string Action { get; init; } = string.Empty;

        [JsonPropertyName("pull_request")] public PullRequest? PullRequest { get; init; }
    }

    public sealed class Search<T> where T : class
    {
        public int TotalCount { get; init; }
        public List<T> Items { get; init; } = new();
    }

    public sealed class Label
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Color { get; init; } = string.Empty;
    }
}

public static class Exceptions
{
    public class ChangelogNotFound : Exception
    {
        public ChangelogNotFound()
        {
        }

        public ChangelogNotFound(string message) : base(message) { }
        public ChangelogNotFound(string message, Exception inner) : base(message, inner) { }
    }

    public class ChangelogIsEmpty : Exception
    {
        public ChangelogIsEmpty()
        {
        }

        public ChangelogIsEmpty(string message) : base(message) { }
        public ChangelogIsEmpty(string message, Exception inner) : base(message, inner) { }
    }
}
