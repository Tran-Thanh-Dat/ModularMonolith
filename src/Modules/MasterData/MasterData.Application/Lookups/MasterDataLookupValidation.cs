namespace MasterData.Application.Lookups;

internal static class MasterDataLookupValidation
{
    public static IReadOnlyList<string> SplitGroupCodes(string groupCodes) =>
        groupCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList();
}
