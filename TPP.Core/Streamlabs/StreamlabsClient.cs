using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Utils;

namespace TPP.Core.Streamlabs;

public class StreamlabsClient
{
    private readonly ILogger<StreamlabsClient> _logger;
    private readonly HttpClient _http;
    public StreamlabsClient(ILogger<StreamlabsClient> logger, string accessToken)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.BaseAddress = new Uri("https://streamlabs.com/api/v2.0/");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// The Streamlabs API puts responses that are lists into an envelope like this. This is probably to have the
    /// top level JSON always be an object, as some (old) JSON parsers don't understand top level lists.
    private record ListEnvelope<T>(List<T> Data);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public record Donation(
        // e.g. 192911565
        // Official documentation and examples say this is a string in json, but that's a lie!
        long DonationId,

        // e.g. 1733865557
        // Similar to the donation ID, it's also a number in JSON, even if streamlabs documentation says otherwise.
        [property: JsonConverter(typeof(InstantAsUnixSecondsConverter))]
        Instant CreatedAt,

        // e.g. "USD" or "EUR"
        string Currency,

        // e.g. "20.0000000000", in currency units (not cents or anything, that's what the fraction is for)
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal Amount,

        // The donor's username (though realistically this can be anything, as it's arbitrary)
        string Name,

        // This theoratically also exists (albeit undocumented), but it's PII, and we don't need it.
        // string Email,

        // The donation's message, which may be absend or just an empty string.
        string? Message
    );

    /// <summary>
    /// Fetch donations for the authenticated user. Results are ordered by creation date, descending.
    /// </summary>
    /// <param name="limit">Limit allows you to limit the number of results output.</param>
    /// <param name="before">The before value is your donation id.</param>
    /// <param name="after">The after value is your donation id.</param>
    /// <param name="currency">The desired currency code. If empty, each record will be in the originating currency.</param>
    /// <param name="verified">If verified is set to 1, response will only include verified donations from paypal,
    /// credit card, skrill and unitpay, if it is set to 0 response will only include streamer added donations from
    /// My Donations page, do not pass this field if you want to include both.</param>
    /// <returns></returns>
    public async Task<List<Donation>> GetDonations(
        int? limit = null,
        int? before = null,
        int? after = null,
        string? currency = null,
        bool? verified = null)
    {
        Dictionary<string, string> queryParams = new();
        if (limit != null) queryParams.Add("limit", limit.Value.ToString());
        if (before != null) queryParams.Add("before", before.Value.ToString());
        if (after != null) queryParams.Add("after", after.Value.ToString());
        if (currency != null) queryParams.Add("currency", currency);
        if (verified != null) queryParams.Add("verified", verified.Value.ToString());

        string queryString = QueryStringBuilder.FromDictionary(queryParams);
        var response = await _http.GetFromJsonAsync<ListEnvelope<Donation>>(
            requestUri: "donations?" + queryString,
            options: SerializerOptions);
        return response!.Data;
    }

    private record SocketTokenEnvelope(string SocketToken);
    /// Allows you to obtain a token which can be used to listen to user's event through sockets.
    public async Task<string> GetSocketToken()
    {
        var response = await _http.GetFromJsonAsync<SocketTokenEnvelope>(
            requestUri: "socket/token",
            options: SerializerOptions);
        return response!.SocketToken;
    }
}
