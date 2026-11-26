using System.Net;

namespace Tellurian.Trains.Drawing.App.Tests.Fakes;

/// <summary>
/// Answers every request with <see cref="Json"/> and <see cref="Status"/>, or fails as unreachable, and records the
/// requests: method, address, API key and body.
/// </summary>
internal sealed class FakeRegistryHandler : HttpMessageHandler
{
    public string Json { get; set; } = string.Empty;
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public bool Unreachable { get; set; }
    public List<Uri> Requested { get; } = [];
    public List<(HttpMethod Method, Uri Address, string? ApiKey, string? Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requested.Add(request.RequestUri!);
        var apiKey = request.Headers.TryGetValues("X-API-Key", out var values) ? values.Single() : null;
        Requests.Add((request.Method, request.RequestUri!, apiKey, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
        if (Unreachable) throw new HttpRequestException("Failed to fetch");
        return new HttpResponseMessage(Status) { Content = new StringContent(Json) };
    }
}
