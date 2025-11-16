using System;
using System.Collections.Generic;

public enum PartyDistributionType
{
    FindersKeepers = 0,
    Random = 1,
    RandomIncludingSpoil = 2,
    ByTurn = 3,
    ByTurnIncludingSpoil = 4
}

public static class PartyDistributionTypeExtensions
{
    private static readonly Dictionary<PartyDistributionType, int> SystemStringIds = new Dictionary<PartyDistributionType, int>
    {
        { PartyDistributionType.FindersKeepers, 487 },
        { PartyDistributionType.Random, 488 },
        { PartyDistributionType.RandomIncludingSpoil, 798 },
        { PartyDistributionType.ByTurn, 799 },
        { PartyDistributionType.ByTurnIncludingSpoil, 800 }
    };

    private static readonly Dictionary<PartyDistributionType, string> DisplayNames = new Dictionary<PartyDistributionType, string>
    {
        { PartyDistributionType.FindersKeepers, "Finders Keepers" },
        { PartyDistributionType.Random, "Random" },
        { PartyDistributionType.RandomIncludingSpoil, "Random Including Spoil" },
        { PartyDistributionType.ByTurn, "By Turn" },
        { PartyDistributionType.ByTurnIncludingSpoil, "By Turn Including Spoil" }
    };

    /// <summary>
    /// Gets the sysstring id used by system messages.
    /// </summary>
    /// <param name="type">The party distribution type</param>
    /// <returns>The sysstring id</returns>
    public static int GetSysStringId(this PartyDistributionType type)
    {
        return SystemStringIds.TryGetValue(type, out int sysStringId) ? sysStringId : -1;
    }

    /// <summary>
    /// Finds the PartyDistributionType by its id
    /// </summary>
    /// <param name="id">The id</param>
    /// <returns>The PartyDistributionType if it is found, null otherwise</returns>
    public static PartyDistributionType? FindById(int id)
    {
        foreach (PartyDistributionType type in Enum.GetValues(typeof(PartyDistributionType)))
        {
            if ((int)type == id)
            {
                return type;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the id used by packets.
    /// </summary>
    /// <param name="type">The party distribution type</param>
    /// <returns>The id</returns>
    public static int GetId(this PartyDistributionType type)
    {
        return (int)type;
    }

    /// <summary>
    /// Gets the display name for the party distribution type
    /// </summary>
    /// <param name="type">The party distribution type</param>
    /// <returns>Human readable display name</returns>
    public static string ToDisplayString(this PartyDistributionType type)
    {
        return DisplayNames.TryGetValue(type, out string displayName) ? displayName : type.ToString();
    }

    /// <summary>
    /// Returns string representation with both enum name and display name
    /// </summary>
    /// <param name="type">The party distribution type</param>
    /// <returns>Formatted string</returns>
    public static string ToString(this PartyDistributionType type)
    {
        return $"{type} ({ToDisplayString(type)})";
    }
}