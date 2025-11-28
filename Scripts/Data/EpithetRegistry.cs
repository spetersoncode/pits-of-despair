namespace PitsOfDespair.Data;

/// <summary>
/// Static registry containing all epithet definitions.
/// Epithets are organized by priority tier based on stat requirements.
/// </summary>
public static class EpithetRegistry
{
    /// <summary>
    /// All available epithets sorted by priority (highest first).
    /// </summary>
    public static readonly EpithetDefinition[] Epithets =
    {
        // === Maxed Single Stats (Priority 1000) - Level 12+ CHARACTER-DEFINING ===
        new() { Name = "Titan", RequiredStr = 12, Priority = 1000 },
        new() { Name = "Wraith", RequiredAgi = 12, Priority = 1000 },
        new() { Name = "Eternal", RequiredEnd = 12, Priority = 1000 },
        new() { Name = "Prophet", RequiredWil = 12, Priority = 1000 },

        // === Quad Balanced (Priority 700) - Level 16+ GENERALIST ===
        new() { Name = "Polymath", RequiredStr = 4, RequiredAgi = 4, RequiredEnd = 4, RequiredWil = 4, Priority = 700 },

        // === Tri-Stat High (Priority 650) - Level 15+ ===
        new() { Name = "Champion", RequiredStr = 5, RequiredAgi = 5, RequiredEnd = 5, Priority = 650 },
        new() { Name = "Undying", RequiredStr = 5, RequiredEnd = 5, RequiredWil = 5, Priority = 650 },
        new() { Name = "Mastermind", RequiredStr = 5, RequiredAgi = 5, RequiredWil = 5, Priority = 650 },
        new() { Name = "Enlightened", RequiredAgi = 5, RequiredEnd = 5, RequiredWil = 5, Priority = 650 },

        // === Dual High (Priority 600) - Level 14+ ===
        new() { Name = "Destroyer", RequiredStr = 7, RequiredEnd = 7, Priority = 600 },
        new() { Name = "Blademaster", RequiredStr = 7, RequiredAgi = 7, Priority = 600 },
        new() { Name = "Conqueror", RequiredStr = 7, RequiredWil = 7, Priority = 600 },
        new() { Name = "Deathblade", RequiredAgi = 7, RequiredWil = 7, Priority = 600 },
        new() { Name = "Aegis", RequiredAgi = 7, RequiredEnd = 7, Priority = 600 },
        new() { Name = "Crusader", RequiredEnd = 7, RequiredWil = 7, Priority = 600 },

        // === Near-Maxed Single (Priority 500) - Level 10+ ===
        new() { Name = "Crusher", RequiredStr = 10, Priority = 500 },
        new() { Name = "Shade", RequiredAgi = 10, Priority = 500 },
        new() { Name = "Fortress", RequiredEnd = 10, Priority = 500 },
        new() { Name = "Visionary", RequiredWil = 10, Priority = 500 },

        // === Tri-Stat Low (Priority 350) - Level 9+ ===
        new() { Name = "Soldier", RequiredStr = 3, RequiredAgi = 3, RequiredEnd = 3, Priority = 350 },
        new() { Name = "Acolyte", RequiredStr = 3, RequiredEnd = 3, RequiredWil = 3, Priority = 350 },
        new() { Name = "Ranger", RequiredStr = 3, RequiredAgi = 3, RequiredWil = 3, Priority = 350 },
        new() { Name = "Monk", RequiredAgi = 3, RequiredEnd = 3, RequiredWil = 3, Priority = 350 },

        // === Dual Mid (Priority 300) - Level 8+ ===
        new() { Name = "Warrior", RequiredStr = 4, RequiredAgi = 4, Priority = 300 },
        new() { Name = "Brute", RequiredStr = 4, RequiredEnd = 4, Priority = 300 },
        new() { Name = "Zealot", RequiredStr = 4, RequiredWil = 4, Priority = 300 },
        new() { Name = "Rogue", RequiredAgi = 4, RequiredWil = 4, Priority = 300 },
        new() { Name = "Guardian", RequiredEnd = 4, RequiredWil = 4, Priority = 300 },
        new() { Name = "Scout", RequiredAgi = 4, RequiredEnd = 4, Priority = 300 },

        // === Dual Early (Priority 150) - Level 4+ ===
        new() { Name = "Scrapper", RequiredStr = 2, RequiredAgi = 2, Priority = 150 },
        new() { Name = "Bruiser", RequiredStr = 2, RequiredEnd = 2, Priority = 150 },
        new() { Name = "Firebrand", RequiredStr = 2, RequiredWil = 2, Priority = 150 },
        new() { Name = "Slippery", RequiredAgi = 2, RequiredEnd = 2, Priority = 150 },
        new() { Name = "Cunning", RequiredAgi = 2, RequiredWil = 2, Priority = 150 },
        new() { Name = "Stoic", RequiredEnd = 2, RequiredWil = 2, Priority = 150 },

        // === Single Mid (Priority 125) - Level 4+ ===
        new() { Name = "Strong", RequiredStr = 4, Priority = 125 },
        new() { Name = "Quick", RequiredAgi = 4, Priority = 125 },
        new() { Name = "Hardy", RequiredEnd = 4, Priority = 125 },
        new() { Name = "Willful", RequiredWil = 4, Priority = 125 },

        // === Single Early (Priority 100) - Level 2+ ===
        new() { Name = "Brawny", RequiredStr = 2, Priority = 100 },
        new() { Name = "Nimble", RequiredAgi = 2, Priority = 100 },
        new() { Name = "Rugged", RequiredEnd = 2, Priority = 100 },
        new() { Name = "Focused", RequiredWil = 2, Priority = 100 },

        // === Default (Priority 0) - Level 1 ===
        new() { Name = "Condemned", Priority = 0 },
    };
}
