using System.Diagnostics;
using OpenTelemetry;

namespace OpenTelemetryDatadogPoC;

public class SensitiveDataProcessor : BaseProcessor<Activity>
{
    private readonly HashSet<string> _sensitiveAttributes = new HashSet<string>
    {
        "http.request.header.Authorization",
        "http.request.header.Cookie",
        "telemetry.sdk.name",
        "url.scheme"
        // Add other sensitive attribute keys as needed
    };

    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.Tags)
        {
            if (_sensitiveAttributes.Contains(tag.Key))
            {
                activity.SetTag(tag.Key, "REDACTED");
            }
        }
        base.OnEnd(activity);
    }
}