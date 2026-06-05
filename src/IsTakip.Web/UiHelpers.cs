namespace IsTakip.Web;

/// <summary>Arayüz yardımcıları: avatar baş harfleri ve isimden türetilen sabit renk.</summary>
public static class UiHelpers
{
    private static readonly string[] AvatarColors =
    {
        "#0C66E4", "#6554C0", "#00857A", "#E2400F", "#C9372C",
        "#0747A6", "#403294", "#206A83", "#974F0C", "#5B7F24",
        "#943D73", "#227D9B"
    };

    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, 1).ToUpperInvariant();
        return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
    }

    public static string AvatarColor(string? key)
    {
        if (string.IsNullOrEmpty(key)) return AvatarColors[0];
        int sum = 0;
        foreach (var ch in key) sum += ch;
        return AvatarColors[sum % AvatarColors.Length];
    }
}
