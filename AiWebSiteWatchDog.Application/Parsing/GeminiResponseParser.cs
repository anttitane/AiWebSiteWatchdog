using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AiWebSiteWatchDog.Application.Parsing
{
    public static class GeminiResponseParser
    {
        // Extracts the first candidates[0].content.parts[0].text, with sensible fallbacks.
        public static string? ExtractText(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                {
                    var cand0 = candidates[0];
                    if (cand0.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                    {
                        var part0 = parts[0];
                        if (part0.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                        {
                            return textEl.GetString();
                        }
                    }
                }

                // Fallback: collect any text fields from all parts
                if (root.TryGetProperty("candidates", out candidates) && candidates.ValueKind == JsonValueKind.Array)
                {
                    var pieces = new List<string>();
                    foreach (var c in candidates.EnumerateArray())
                    {
                        if (c.TryGetProperty("content", out var content2) && content2.TryGetProperty("parts", out var parts2) && parts2.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var p in parts2.EnumerateArray())
                            {
                                if (p.TryGetProperty("text", out var textEl2) && textEl2.ValueKind == JsonValueKind.String)
                                {
                                    var t = textEl2.GetString();
                                    if (!string.IsNullOrEmpty(t)) pieces.Add(t);
                                }
                            }
                        }
                    }
                    if (pieces.Count > 0) return string.Join("\n", pieces);
                }
            }
            catch
            {
                // Ignore parse errors, fallback below
            }
            return json; // if structure unexpected, return original content
        }
    }
}
