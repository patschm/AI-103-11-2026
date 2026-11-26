using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;

internal class Program
{
    // Configuration
    private const string ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/openai/v1";
    private const string MODEL_DEPLOYMENT = "sora-2";

    // Entra ID auth via Azure CLI sign-in (the OpenAI .NET SDK has no video client, so we call the REST API)
    private static readonly AzureCliCredential Credential = new();
    private static readonly TokenRequestContext TokenContext = new(["https://cognitiveservices.azure.com/.default"]);
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(ENDPOINT.TrimEnd('/') + "/") };

    private static async Task Main(string[] args)
    {
        // Demo 1: generate a video from a text prompt
        var originalId = await GenerateVideoDemoAsync();

        // Demo 2: remix the generated video
       await RemixVideoDemoAsync(originalId);

        // Demo 3: generate a video from a reference image
        await ImageToVideoDemoAsync();

        Console.WriteLine("\n=== Done ===");
    }

    // Demo 1: text-to-video. Returns the id of the completed video, or null if it did not complete.
    private static async Task<string?> GenerateVideoDemoAsync()
    {
        var video = await CreateVideoAsync(
            "A peaceful mountain lake at sunrise with mist rising from the water",
            "1280x720", "4");
        video = await PollVideoStatusAsync(video["id"]!.GetValue<string>());

        if (video["status"]?.GetValue<string>() != "completed")
            return null;

        var videoId = video["id"]!.GetValue<string>();
        await DownloadVideoAsync(videoId, "original_video.mp4");
        return videoId;
    }

    // Demo 2: remix an existing video with a different style
    private static async Task RemixVideoDemoAsync(string originalId)
    {
        var remixed = await RemixVideoAsync(
            originalId, "Shift the color palette to warm sunset tones with golden light");
        if (remixed["status"]?.GetValue<string>() == "completed")
            await DownloadVideoAsync(remixed["id"]!.GetValue<string>(), "remixed_video.mp4");
    }

    // Demo 3: image-to-video using a reference image as the starting frame
    private static async Task ImageToVideoDemoAsync()
    {
        var imageVideo = await GenerateVideoFromImageAsync(
            "reference.png", "The scene comes to life with gentle movement and ambient lighting", "1280x720", "4");
        if (imageVideo["status"]?.GetValue<string>() == "completed")
            await DownloadVideoAsync(imageVideo["id"]!.GetValue<string>(), "image_based_video.mp4");
    }

    private static async Task<HttpRequestMessage> AuthorizeAsync(HttpRequestMessage request)
    {
        var token = await Credential.GetTokenAsync(TokenContext, CancellationToken.None);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return request;
    }

    private static async Task<JsonNode> SendJsonAsync(HttpRequestMessage request)
    {
        using var response = await Http.SendAsync(await AuthorizeAsync(request));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        return JsonNode.Parse(body)!;
    }

    private static Task<JsonNode> CreateVideoAsync(string prompt, string size, string seconds)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "videos")
        {
            Content = JsonContent.Create(new { model = MODEL_DEPLOYMENT, prompt, size, seconds }),
        };
        return SendJsonAsync(request);
    }

    // Poll the video status every 20 seconds until it completes or fails
    private static async Task<JsonNode> PollVideoStatusAsync(string videoId)
    {
        var video = await SendJsonAsync(new HttpRequestMessage(HttpMethod.Get, $"videos/{videoId}"));
        while (video["status"]?.GetValue<string>() is not ("completed" or "failed" or "cancelled"))
        {
            Console.WriteLine($"Status: {video["status"]}. Waiting 20 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(20));
            video = await SendJsonAsync(new HttpRequestMessage(HttpMethod.Get, $"videos/{videoId}"));
        }

        if (video["status"]!.GetValue<string>() == "completed")
            Console.WriteLine("Video successfully completed!");
        else
            Console.WriteLine($"Video creation ended with status: {video["status"]}");
        return video;
    }

    // Create a remix of an existing video with a new prompt
    private static async Task<JsonNode> RemixVideoAsync(string videoId, string prompt)
    {
        Console.WriteLine($"Starting video remix for: {videoId}");
        var remix = await SendJsonAsync(new HttpRequestMessage(HttpMethod.Post, $"videos/{videoId}/remix")
        {
            Content = JsonContent.Create(new { prompt }),
        });
        Console.WriteLine($"Remix started. New video ID: {remix["id"]}");
        Console.WriteLine($"Initial status: {remix["status"]}");
        return await PollVideoStatusAsync(remix["id"]!.GetValue<string>());
    }

    // Download the completed video to a local file
    private static async Task DownloadVideoAsync(string videoId, string outputFilename)
    {
        Console.WriteLine($"Downloading video {videoId}...");
        using var request = await AuthorizeAsync(
            new HttpRequestMessage(HttpMethod.Get, $"videos/{videoId}/content?variant=video"));
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var file = File.Create(outputFilename);
        await response.Content.CopyToAsync(file);
        Console.WriteLine($"Saved video to {outputFilename}");
    }

    // Generate a video using a reference image as the starting frame
    private static async Task<JsonNode> GenerateVideoFromImageAsync(string imagePath, string prompt, string size, string seconds)
    {
        Console.WriteLine($"Starting video generation from image: {imagePath}");

        var imageContent = new ByteArrayContent(await File.ReadAllBytesAsync(imagePath));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { new StringContent(MODEL_DEPLOYMENT), "model" },
            { new StringContent(prompt), "prompt" },
            { new StringContent(size), "size" },
            { new StringContent(seconds), "seconds" },
            { imageContent, "input_reference", Path.GetFileName(imagePath) },
        };

        var video = await SendJsonAsync(new HttpRequestMessage(HttpMethod.Post, "videos") { Content = form });
        Console.WriteLine($"Video creation started. ID: {video["id"]}");
        Console.WriteLine($"Initial status: {video["status"]}");
        return await PollVideoStatusAsync(video["id"]!.GetValue<string>());
    }
}
