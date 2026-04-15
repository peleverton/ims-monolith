using Serilog.Core;
using Serilog.Events;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Serilog destructuring policy that redacts sensitive data from log output.
/// Prevents passwords, tokens, secrets, and other sensitive fields from appearing in logs.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "senha",
        "secret",
        "secretkey",
        "token",
        "accesstoken",
        "refreshtoken",
        "jwt",
        "authorization",
        "apikey",
        "connectionstring",
        "creditcard",
        "cardnumber",
        "cvv",
        "ssn",
        "cpf"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;

        if (value is not IDictionary<string, object> dict)
            return false;

        var sanitized = new List<LogEventProperty>();

        foreach (var kvp in dict)
        {
            if (SensitivePropertyNames.Contains(kvp.Key))
            {
                sanitized.Add(new LogEventProperty(kvp.Key, propertyValueFactory.CreatePropertyValue("[REDACTED]")));
            }
            else
            {
                sanitized.Add(new LogEventProperty(kvp.Key, propertyValueFactory.CreatePropertyValue(kvp.Value, destructureObjects: true)));
            }
        }

        result = new StructureValue(sanitized);
        return true;
    }
}
