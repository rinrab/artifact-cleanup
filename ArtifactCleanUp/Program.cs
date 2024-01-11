using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ArtifactCleanUp
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Please create GitHub access token with scope 'Update GitHub Action workflows'.");
            Console.WriteLine("You can create it here: https://github.com/settings/tokens");
            Console.WriteLine();

            Console.Write("Enter your access token (ghp_***): ");
            string token = Console.ReadLine()!;

            Console.Write("Enter repository (owner/repo): ");
            string repo = Console.ReadLine()!;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ArtifactsCleaner/1.0");
                httpClient.BaseAddress = new Uri("https://api.github.com");

                Console.WriteLine("Looking for artifacts...");

                List<Artifact> artifacts = await ListArtifacts(httpClient, repo);
                artifacts.Reverse(); // Start from old artifacts

                Console.Write("Found: {0} artifacts in repository {1}. Do you want to delete them? (y/n): ", artifacts.Count, repo);

                if (Console.ReadLine() != "y")
                {
                    return;
                }

                foreach (Artifact artifact in artifacts)
                {
                    Console.WriteLine("Deleting artifact {0}...", artifact.Id);
                    await DeleteArtifactAsync(httpClient, repo, artifact.Id);
                }

                Console.WriteLine("Artifacts deleted!!");
            }
        }

        private static async Task<List<Artifact>> ListArtifacts(HttpClient httpClient, string repo)
        {
            List<Artifact> rv = [];
            int pageIndex = 1;
            int pageSize = 100;

            while (true)
            {
                string url = $"/repos/{repo}/actions/artifacts?per_page={pageSize}&page={pageIndex}";
                ListArtifactsResponse? page = await httpClient.GetFromJsonAsync<ListArtifactsResponse>(url);

                if (page != null)
                {
                    rv.AddRange(page.Artifacts);
                    pageIndex++;
                }
                else
                {
                    throw new Exception("Page is null");
                }

                if (page.Artifacts.Length < pageSize)
                {
                    break;
                }
            }

            return rv;
        }

        private static async Task DeleteArtifactAsync(HttpClient httpClient, string repo, int id)
        {
            await httpClient.DeleteAsync($"repos/{repo}/actions/artifacts/{id}");
        }

        public class ListArtifactsResponse
        {
            [JsonPropertyName("artifacts")]
            public required Artifact[] Artifacts { get; set; }
        }

        public class Artifact
        {
            [JsonPropertyName("id")]
            public required int Id { get; set; }
        }
    }
}