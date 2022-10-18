using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ChangeMaster;

public sealed class Github
{
    private readonly HttpClient _httpClient;

    public Github(string token)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PostmanRuntime/7.28.0");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.groot-preview+json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static string BaseUrl { get; } = "https://api.github.com";

    public async Task<GithubModels.Search<GithubModels.PullRequest>> FetchPullRequests(string repository,
        DateTime mergedAfter,
        string label, int page)
    {
        var url =
            $"{BaseUrl}/search/issues?q=repo:{repository} is:pr is:merged merged:>={mergedAfter.ToString("yyyy-MM-dd")} label:\"{label}\"&order=desc&per_page=100&sort=created&page={page}";
        var response = await _httpClient.GetAsync(url) ?? throw new InvalidOperationException("Invalid response");

        return await response.Content.ReadFromJsonAsync<GithubModels.Search<GithubModels.PullRequest>>(
            Settings.JsonOptions) ?? throw new InvalidOperationException("Parsing response error");
    }


    public async Task AddLabel(string repository, int pullRequest, string label)
    {
        var url = $"{BaseUrl}/repos/{repository}/issues/{pullRequest}/labels";
        await _httpClient.PostAsync(url, new StringContent($"{{ \"labels\": [\"{label}\"] }}"));
    }

    public async Task RemoveLabel(string repository, int pullRequest, string label)
    {
        var url = $"{BaseUrl}/repos/{repository}/issues/{pullRequest}/labels/{Uri.EscapeDataString(label)}";
        await _httpClient.DeleteAsync(url);
    }
}
