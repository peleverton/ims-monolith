using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.Webhooks.Application;

/// <summary>
/// US-069: Assina payloads de webhook com HMAC-SHA256 (padrão GitHub/Stripe).
/// Header: X-IMS-Signature: sha256=&lt;hex&gt;
/// </summary>
public static class WebhookSigner
{
    public static string Sign(string secret, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string secret, string payload, string signature) =>
        string.Equals(Sign(secret, payload), signature, StringComparison.OrdinalIgnoreCase);
}
