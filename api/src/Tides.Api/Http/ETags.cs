using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace Tides.Api.Http;

public static class ETags
{
    // Hashes the value's JSON, so equal responses get the same ETag.
    public static string Compute<T>(T value)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value));
        return $"\"{Convert.ToHexString(hash)}\"";
    }

    // If-None-Match uses weak comparison, so W/"x" matches "x".
    public static bool Matches(HttpRequest request, string eTag)
    {
        var current = new EntityTagHeaderValue(eTag);

        return request.GetTypedHeaders().IfNoneMatch.Any(tag =>
            tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(current, useStrongComparison: false));
    }
}
