/**
 * jsfxr preset metadata and descriptions.
 */

import type { JsfxrPreset, SoundCategory } from './types.js';

/**
 * Metadata for a jsfxr preset.
 */
export interface PresetInfo {
  id: JsfxrPreset;
  name: string;
  description: string;
  typicalUses: string[];
  suggestedCategories: SoundCategory[];
}

/**
 * All available presets with their metadata.
 */
export const PRESET_INFO: readonly PresetInfo[] = [
  {
    id: 'pickupCoin',
    name: 'Pickup/Coin',
    description: 'Bright, rising tones suitable for collecting items or rewards',
    typicalUses: ['Collecting coins', 'Picking up items', 'Gaining resources', 'Small rewards'],
    suggestedCategories: ['Items', 'UI'],
  },
  {
    id: 'laserShoot',
    name: 'Laser/Shoot',
    description: 'Sharp, descending tones for projectiles and ranged attacks',
    typicalUses: ['Ranged attacks', 'Projectiles', 'Energy weapons', 'Casting spells'],
    suggestedCategories: ['Combat', 'Magic'],
  },
  {
    id: 'explosion',
    name: 'Explosion',
    description: 'Noise-based impacts for explosions and heavy hits',
    typicalUses: ['Explosions', 'Heavy impacts', 'Destruction', 'Death effects'],
    suggestedCategories: ['Combat', 'Deaths', 'Environment'],
  },
  {
    id: 'powerUp',
    name: 'Power Up',
    description: 'Rising, energetic tones for buffs and enhancements',
    typicalUses: ['Level up', 'Gaining buffs', 'Healing', 'Ability activation'],
    suggestedCategories: ['Player', 'Items', 'Magic'],
  },
  {
    id: 'hitHurt',
    name: 'Hit/Hurt',
    description: 'Quick, punchy impacts for damage and melee hits',
    typicalUses: ['Melee attacks', 'Taking damage', 'Blocking', 'Physical impacts'],
    suggestedCategories: ['Combat', 'Player', 'Enemy'],
  },
  {
    id: 'jump',
    name: 'Jump',
    description: 'Quick rising tones for movement and actions',
    typicalUses: ['Jumping', 'Dodging', 'Quick movements', 'Activation'],
    suggestedCategories: ['Player', 'UI'],
  },
  {
    id: 'blipSelect',
    name: 'Blip/Select',
    description: 'Short, clean tones for UI interactions',
    typicalUses: ['Menu selection', 'Button clicks', 'UI feedback', 'Notifications'],
    suggestedCategories: ['UI'],
  },
  {
    id: 'synth',
    name: 'Synth',
    description: 'Musical, sustained tones for ambient effects',
    typicalUses: ['Ambient sounds', 'Musical cues', 'Atmosphere', 'Background effects'],
    suggestedCategories: ['Environment', 'Magic'],
  },
  {
    id: 'tone',
    name: 'Tone',
    description: 'Pure sine wave tone, good starting point for custom sounds',
    typicalUses: ['Base for custom sounds', 'Alert tones', 'Simple notifications'],
    suggestedCategories: ['UI'],
  },
  {
    id: 'click',
    name: 'Click',
    description: 'Very short, sharp sounds for UI and mechanical effects',
    typicalUses: ['Button clicks', 'Mechanical sounds', 'Footsteps base', 'Quick impacts'],
    suggestedCategories: ['UI', 'Environment'],
  },
  {
    id: 'random',
    name: 'Random',
    description: 'Completely randomized parameters for experimentation',
    typicalUses: ['Experimentation', 'Finding new sounds', 'Happy accidents'],
    suggestedCategories: ['Combat', 'Magic', 'Items', 'Environment'],
  },
] as const;

/**
 * Get preset info by ID.
 */
export function getPresetInfo(preset: JsfxrPreset): PresetInfo | undefined {
  return PRESET_INFO.find((p) => p.id === preset);
}

/**
 * Get presets suggested for a category.
 */
export function getPresetsForCategory(category: SoundCategory): PresetInfo[] {
  return PRESET_INFO.filter((p) => p.suggestedCategories.includes(category));
}

/**
 * Category descriptions for the CLI.
 */
export const CATEGORY_INFO: Record<SoundCategory, string> = {
  Combat: 'Melee hits, ranged attacks, blocks, parries',
  Magic: 'Spells, enchantments, magical effects',
  Items: 'Pickups, potions, equipment sounds',
  UI: 'Menu navigation, feedback, notifications',
  Environment: 'Doors, traps, ambient, environmental',
  Deaths: 'Player and creature death sounds',
  Player: 'Player-specific actions and feedback',
  Enemy: 'Enemy-specific sounds and attacks',
};
