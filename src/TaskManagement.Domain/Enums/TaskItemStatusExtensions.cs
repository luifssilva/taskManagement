using System.Globalization;
using System.Text;

namespace TaskManagement.Domain.Enums;

/// <summary>
/// Converts <see cref="TaskItemStatus"/> values to and from their public (display) names:
/// "Pendente", "Em progresso" and "Concluída".
/// </summary>
public static class TaskItemStatusExtensions
{
    private static readonly IReadOnlyDictionary<TaskItemStatus, string> DisplayNames =
        new Dictionary<TaskItemStatus, string>
        {
            [TaskItemStatus.Pendente] = "Pendente",
            [TaskItemStatus.EmProgresso] = "Em progresso",
            [TaskItemStatus.Concluida] = "Concluída"
        };

    /// <summary>Public names accepted and returned by the API.</summary>
    public static IReadOnlyCollection<string> AllowedDisplayNames { get; } = DisplayNames.Values.ToArray();

    public static string ToDisplayName(this TaskItemStatus status) =>
        DisplayNames.TryGetValue(status, out var name)
            ? name
            : throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown task status.");

    /// <summary>
    /// Parses a status name. Matching ignores case, accents and whitespace, so
    /// "Concluída", "concluida" and "EmProgresso" are all accepted. Numeric values are rejected.
    /// </summary>
    public static bool TryParse(string? value, out TaskItemStatus status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Normalize(value);

        foreach (var (candidate, displayName) in DisplayNames)
        {
            if (Normalize(displayName) == normalized || Normalize(candidate.ToString()) == normalized)
            {
                status = candidate;
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsWhiteSpace(character) ||
                CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
