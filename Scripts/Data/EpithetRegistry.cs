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
        new()
        {
            Name = "Titan",
            RequiredStr = 12,
            Priority = 1000,
            Description = "A living engine of destruction, muscles corded with unnatural power. The dungeon's crucible has forged a being of pure, devastating might."
        },
        new()
        {
            Name = "Wraith",
            RequiredAgi = 12,
            Priority = 1000,
            Description = "More shadow than flesh, moving between moments. The darkness has claimed this one as its own, granting passage through spaces others cannot perceive."
        },
        new()
        {
            Name = "Eternal",
            RequiredEnd = 12,
            Priority = 1000,
            Description = "A body that has forgotten how to die. Wounds close as quickly as they open, and exhaustion is a distant memory. The abyss could not break what it could not kill."
        },
        new()
        {
            Name = "Prophet",
            RequiredWil = 12,
            Priority = 1000,
            Description = "Eyes that see beyond the veil of flesh and stone. The mind has transcended mortal limits, perceiving truths that would shatter lesser souls."
        },

        // === Quad Balanced (Priority 700) - Level 16+ GENERALIST ===
        new()
        {
            Name = "Polymath",
            RequiredStr = 4,
            RequiredAgi = 4,
            RequiredEnd = 4,
            RequiredWil = 4,
            Priority = 700,
            Description = "A rare soul who refused to specialize, instead mastering every aspect of survival. Balanced in all things, limited by none."
        },

        // === Tri-Stat High (Priority 650) - Level 15+ ===
        new()
        {
            Name = "Champion",
            RequiredStr = 5,
            RequiredAgi = 5,
            RequiredEnd = 5,
            Priority = 650,
            Description = "The complete warrior, honed to physical perfection. Strong, swift, and tireless—a living weapon shaped by countless battles."
        },
        new()
        {
            Name = "Undying",
            RequiredStr = 5,
            RequiredEnd = 5,
            RequiredWil = 5,
            Priority = 650,
            Description = "Sheer stubbornness made manifest. Too strong to fall, too tough to stay down, too willful to accept defeat. Death itself seems uncertain."
        },
        new()
        {
            Name = "Mastermind",
            RequiredStr = 5,
            RequiredAgi = 5,
            RequiredWil = 5,
            Priority = 650,
            Description = "A cunning predator who combines lethal capability with piercing insight. Every movement calculated, every strike deliberate."
        },
        new()
        {
            Name = "Enlightened",
            RequiredAgi = 5,
            RequiredEnd = 5,
            RequiredWil = 5,
            Priority = 650,
            Description = "Grace, resilience, and clarity united in perfect harmony. The body flows like water while the mind remains still as stone."
        },

        // === Dual High (Priority 600) - Level 14+ ===
        new()
        {
            Name = "Destroyer",
            RequiredStr = 7,
            RequiredEnd = 7,
            Priority = 600,
            Description = "An unstoppable force wrapped in unyielding flesh. Charges through opposition, shrugging off blows that would fell lesser beings."
        },
        new()
        {
            Name = "Blademaster",
            RequiredStr = 7,
            RequiredAgi = 7,
            Priority = 600,
            Description = "Power and precision fused into deadly artistry. Each strike flows with devastating grace, turning combat into a lethal dance."
        },
        new()
        {
            Name = "Conqueror",
            RequiredStr = 7,
            RequiredWil = 7,
            Priority = 600,
            Description = "Raw might guided by unshakeable purpose. Dominates through force of arm and force of will alike, bending opposition to heel."
        },
        new()
        {
            Name = "Deathblade",
            RequiredAgi = 7,
            RequiredWil = 7,
            Priority = 600,
            Description = "A silent executioner who strikes from shadow with terrible precision. The mind plots while the body delivers swift endings."
        },
        new()
        {
            Name = "Aegis",
            RequiredAgi = 7,
            RequiredEnd = 7,
            Priority = 600,
            Description = "Untouchable in motion, unbreakable when still. Evades what can be dodged, endures what cannot. A perfect defensive form."
        },
        new()
        {
            Name = "Crusader",
            RequiredEnd = 7,
            RequiredWil = 7,
            Priority = 600,
            Description = "An unbreakable pillar of resolve. The flesh refuses to fail, the spirit refuses to falter. Advances through any opposition."
        },

        // === Near-Maxed Single (Priority 500) - Level 10+ ===
        new()
        {
            Name = "Crusher",
            RequiredStr = 10,
            Priority = 500,
            Description = "Bones groan and stone cracks beneath those fists. Raw physical power approaches its terrifying peak."
        },
        new()
        {
            Name = "Shade",
            RequiredAgi = 10,
            Priority = 500,
            Description = "A blur at the edge of vision, a whisper of displaced air. Nearly invisible in motion, striking from angles that should not exist."
        },
        new()
        {
            Name = "Fortress",
            RequiredEnd = 10,
            Priority = 500,
            Description = "Scarred flesh that refuses to yield, a body hardened beyond natural limits. Pain has become a distant whisper."
        },
        new()
        {
            Name = "Visionary",
            RequiredWil = 10,
            Priority = 500,
            Description = "The mind burns with clarity that borders on precognition. Patterns emerge from chaos, and the dungeon's secrets grow transparent."
        },

        // === Tri-Stat Low (Priority 350) - Level 9+ ===
        new()
        {
            Name = "Soldier",
            RequiredStr = 3,
            RequiredAgi = 3,
            RequiredEnd = 3,
            Priority = 350,
            Description = "A disciplined survivor who balances strength, speed, and stamina. The fundamentals of combat are becoming second nature."
        },
        new()
        {
            Name = "Acolyte",
            RequiredStr = 3,
            RequiredEnd = 3,
            RequiredWil = 3,
            Priority = 350,
            Description = "Power, endurance, and focus combine in equal measure. A devoted student of survival, learning the dungeon's harsh lessons."
        },
        new()
        {
            Name = "Ranger",
            RequiredStr = 3,
            RequiredAgi = 3,
            RequiredWil = 3,
            Priority = 350,
            Description = "Alert and capable, blending physical prowess with sharp instincts. Adapts to threats with predatory awareness."
        },
        new()
        {
            Name = "Monk",
            RequiredAgi = 3,
            RequiredEnd = 3,
            RequiredWil = 3,
            Priority = 350,
            Description = "Body and mind working in concert, finding balance through adversity. Movement flows with meditative purpose."
        },

        // === Dual Mid (Priority 300) - Level 8+ ===
        new()
        {
            Name = "Warrior",
            RequiredStr = 4,
            RequiredAgi = 4,
            Priority = 300,
            Description = "Strength and speed honing into martial competence. The awkwardness of inexperience gives way to deadly efficiency."
        },
        new()
        {
            Name = "Brute",
            RequiredStr = 4,
            RequiredEnd = 4,
            Priority = 300,
            Description = "Power and durability building into something formidable. Takes hits and gives them back harder."
        },
        new()
        {
            Name = "Zealot",
            RequiredStr = 4,
            RequiredWil = 4,
            Priority = 300,
            Description = "Driven by fierce conviction backed by growing strength. An intensity burns in those eyes that lesser foes find unnerving."
        },
        new()
        {
            Name = "Rogue",
            RequiredAgi = 4,
            RequiredWil = 4,
            Priority = 300,
            Description = "Quick and cunning, preferring cleverness to brute force. Opportunities appear where others see only obstacles."
        },
        new()
        {
            Name = "Guardian",
            RequiredEnd = 4,
            RequiredWil = 4,
            Priority = 300,
            Description = "Tough in body, steady in spirit. Endures what others cannot, holding the line through sheer determination."
        },
        new()
        {
            Name = "Scout",
            RequiredAgi = 4,
            RequiredEnd = 4,
            Priority = 300,
            Description = "Swift and hardy, built for the long hunt. Covers ground tirelessly, evading what cannot be outrun."
        },

        // === Dual Early (Priority 150) - Level 4+ ===
        new()
        {
            Name = "Scrapper",
            RequiredStr = 2,
            RequiredAgi = 2,
            Priority = 150,
            Description = "A brawler learning to fight dirty and move fast. Survival instincts sharpen with each desperate encounter."
        },
        new()
        {
            Name = "Bruiser",
            RequiredStr = 2,
            RequiredEnd = 2,
            Priority = 150,
            Description = "Growing stronger and tougher through constant abuse. The dungeon's beatings are becoming less effective."
        },
        new()
        {
            Name = "Firebrand",
            RequiredStr = 2,
            RequiredWil = 2,
            Priority = 150,
            Description = "Passionate fury matched with developing might. Refuses to go quietly into the dark."
        },
        new()
        {
            Name = "Slippery",
            RequiredAgi = 2,
            RequiredEnd = 2,
            Priority = 150,
            Description = "Hard to catch, harder to put down. Learning that evasion and persistence outlast raw aggression."
        },
        new()
        {
            Name = "Cunning",
            RequiredAgi = 2,
            RequiredWil = 2,
            Priority = 150,
            Description = "Quick feet and quicker wits make for a dangerous combination. Watching, learning, adapting."
        },
        new()
        {
            Name = "Stoic",
            RequiredEnd = 2,
            RequiredWil = 2,
            Priority = 150,
            Description = "Endures hardship with quiet resolve. Neither pain nor fear will break this stubborn spirit."
        },

        // === Single Mid (Priority 125) - Level 4+ ===
        new()
        {
            Name = "Strong",
            RequiredStr = 4,
            Priority = 125,
            Description = "Muscles hardening, blows landing heavier. The path of might reveals itself through broken enemies."
        },
        new()
        {
            Name = "Quick",
            RequiredAgi = 4,
            Priority = 125,
            Description = "Reactions sharpening, movements becoming fluid. Speed is becoming a reliable ally in the dark."
        },
        new()
        {
            Name = "Hardy",
            RequiredEnd = 4,
            Priority = 125,
            Description = "Scars accumulate but the body only grows tougher. What once wounded now merely stings."
        },
        new()
        {
            Name = "Willful",
            RequiredWil = 4,
            Priority = 125,
            Description = "The mind grows focused and resistant. Distractions and fears fade before sharpening resolve."
        },

        // === Single Early (Priority 100) - Level 2+ ===
        new()
        {
            Name = "Brawny",
            RequiredStr = 2,
            Priority = 100,
            Description = "Developing raw strength through brutal necessity. Each swing carries more weight than before."
        },
        new()
        {
            Name = "Nimble",
            RequiredAgi = 2,
            Priority = 100,
            Description = "Learning to move with purpose, to slip past danger. Clumsiness gives way to cautious grace."
        },
        new()
        {
            Name = "Rugged",
            RequiredEnd = 2,
            Priority = 100,
            Description = "Toughening against the dungeon's abuse. Pain is becoming a familiar companion rather than a shock."
        },
        new()
        {
            Name = "Focused",
            RequiredWil = 2,
            Priority = 100,
            Description = "Sharpening concentration cuts through the fog of fear. Clarity emerges from the chaos."
        },

        // === Default (Priority 0) - Level 1 ===
        new()
        {
            Name = "Condemned",
            Priority = 0,
            Description = "A condemned prisoner, exiled to die in these forsaken depths. Weary but determined to survive."
        },
    };
}
