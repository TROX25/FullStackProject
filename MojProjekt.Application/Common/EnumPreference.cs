namespace MojProjekt.Application.Common;

/// <summary>
/// Represents a user preference for an enum-valued attribute extracted from a natural-language query,
/// e.g. "preferably automatic" (IsRequired = false) vs. "must be automatic" (IsRequired = true).
/// </summary>
public sealed record EnumPreference<T>(T Value, bool IsRequired) where T : struct, Enum;
