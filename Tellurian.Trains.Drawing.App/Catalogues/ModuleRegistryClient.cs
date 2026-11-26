using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tellurian.Trains.Drawing.App.Catalogues;

/// <summary>
/// The Module Registry's API, on another origin, which allows this app's (CORS):
/// <list type="bullet">
/// <item>its catalogue (CA-5), the module standards and end profiles as a catalogue file, imported as any catalogue file
/// is (CA-3); public, without API key;</item>
/// <item>saving an end profile drawing (EX-11), with the user's API key (AP-8), for users who may manage end profiles.</item>
/// </list>
/// </summary>
public class ModuleRegistryClient(HttpClient http)
{
    /// <summary>The Registry's API.</summary>
    public const string ApiUrl = "https://moduleregistry.azurewebsites.net/api";

    /// <summary>The Registry's catalogue, <c>GET /api/catalogue</c>, with the optional filters <c>scale</c> and <c>country</c>.</summary>
    public const string CatalogueUrl = ApiUrl + "/catalogue";

    /// <summary>The header with the user's API key.</summary>
    public const string ApiKeyHeader = "X-API-Key";

    /// <summary>
    /// The name the fetched catalogue goes by in import problems: a catalogue file's, so an answer that isn't a
    /// catalogue file is reported rather than skipped.
    /// </summary>
    public const string SourceName = "Module Registry.catalogue.json";

    /// <summary>The address to fetch, limited to <paramref name="scale"/> (a short name) and <paramref name="country"/> (two letters) when given.</summary>
    public static Uri Address(string? scale, string? country)
    {
        var query = new[] { ("scale", scale), ("country", country) }
            .Where(q => !string.IsNullOrWhiteSpace(q.Item2))
            .Select(q => $"{q.Item1}={Uri.EscapeDataString(q.Item2!.Trim())}")
            .ToList();
        return new(query.Count == 0 ? CatalogueUrl : $"{CatalogueUrl}?{string.Join('&', query)}");
    }

    /// <summary>Where the drawing of the Registry's end profile <paramref name="endProfileId"/> is saved: <c>POST /api/endprofiles/{id}/drawing</c>.</summary>
    public static Uri EndProfileDrawingAddress(int endProfileId) => new($"{ApiUrl}/endprofiles/{endProfileId}/drawing");

    /// <summary>The catalogue file's text.</summary>
    /// <exception cref="ModuleRegistryException">The Registry couldn't be reached or didn't answer with the catalogue.</exception>
    public virtual async Task<string> FetchAsync(string? scale, string? country, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, Address(scale, country)), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new ModuleRegistryException($"{(int)response.StatusCode} {response.ReasonPhrase}");
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Saves an end profile file (<paramref name="json"/>) as the drawing of the Registry's end profile
    /// <paramref name="endProfileId"/>, for the user of <paramref name="apiKey"/>.
    /// </summary>
    /// <returns>How it went; the Registry refuses a drawing whose revision isn't higher than the one it has.</returns>
    /// <exception cref="ModuleRegistryException">The Registry couldn't be reached.</exception>
    public virtual async Task<EndProfileUploadResult> UploadEndProfileDrawingAsync(int endProfileId, string json, string apiKey, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, EndProfileDrawingAddress(endProfileId))
        {
            Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
        };
        request.Headers.Add(ApiKeyHeader, apiKey);
        using var response = await SendAsync(request, cancellationToken);
        var status = response.StatusCode switch
        {
            HttpStatusCode.OK => EndProfileUploadStatus.Saved,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => EndProfileUploadStatus.NotAllowed,
            HttpStatusCode.NotFound => EndProfileUploadStatus.NotFound,
            HttpStatusCode.Conflict => EndProfileUploadStatus.Conflict,
            HttpStatusCode.RequestEntityTooLarge => EndProfileUploadStatus.TooLarge,
            _ => EndProfileUploadStatus.Refused,
        };
        return new(status, status == EndProfileUploadStatus.Saved ? null : await ProblemTitleAsync(response, cancellationToken) ?? $"{(int)response.StatusCode} {response.ReasonPhrase}");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            try
            {
                return await http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new ModuleRegistryException(ex.Message, ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ModuleRegistryException(ex.Message, ex);
            }
        }
    }

    /// <summary>The title of a problem details answer, in English, if the answer is one.</summary>
    private static async Task<string?> ProblemTitleAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                ? title.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>How saving an end profile drawing in the Module Registry went.</summary>
public enum EndProfileUploadStatus
{
    Saved,
    /// <summary>The API key is missing or wrong, or its user may not manage end profiles.</summary>
    NotAllowed,
    /// <summary>The Registry has no end profile with the id.</summary>
    NotFound,
    /// <summary>The Registry has a drawing with the same or a higher revision, perhaps saved by someone else.</summary>
    Conflict,
    TooLarge,
    /// <summary>The Registry didn't accept the file, e.g. it refers to another end profile.</summary>
    Refused,
    /// <summary>Not sent: the end profile has no Registry id in its external references.</summary>
    NotInRegistry,
    /// <summary>Not sent: there is no API key in the settings.</summary>
    NoApiKey,
}

/// <summary>The status, and the Registry's explanation when it didn't save the drawing.</summary>
public sealed record EndProfileUploadResult(EndProfileUploadStatus Status, string? Message = null);

/// <summary>The Module Registry couldn't be reached or didn't answer as expected.</summary>
public sealed class ModuleRegistryException(string message, Exception? innerException = null) : Exception(message, innerException);
