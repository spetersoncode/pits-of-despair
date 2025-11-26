/**
 * Parameter schemas and documentation for jsfxr.
 */

/**
 * Parameter info for documentation.
 */
export interface ParameterInfo {
  name: string;
  description: string;
  range: { min: number; max: number };
  default: number;
  signed: boolean;
  category: string;
  tips?: string[];
}

/**
 * All jsfxr parameters with documentation.
 */
export const PARAMETER_INFO: Record<string, ParameterInfo> = {
  wave_type: {
    name: 'Wave Type',
    description: 'The waveform shape: 0=Square, 1=Sawtooth, 2=Sine, 3=Noise, 4=Triangle, 5=PinkNoise, 6=Tan, 7=Whistle',
    range: { min: 0, max: 7 },
    default: 0,
    signed: false,
    category: 'wave',
    tips: [
      'Square (0): Classic retro sound, good for blips and UI',
      'Sawtooth (1): Brighter, edgier sound, good for lasers',
      'Sine (2): Pure tone, good for base or soft sounds',
      'Noise (3): White noise, essential for explosions and hits',
    ],
  },
  p_env_attack: {
    name: 'Attack',
    description: 'Time for sound to reach full volume',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'envelope',
    tips: [
      'Low (0-0.1): Instant start, punchy sounds',
      'High (0.3+): Gradual swell, ambient sounds',
    ],
  },
  p_env_sustain: {
    name: 'Sustain',
    description: 'How long the sound holds at full volume',
    range: { min: 0, max: 1 },
    default: 0.3,
    signed: false,
    category: 'envelope',
    tips: [
      'Short: Quick blips and hits',
      'Long: Sustained tones and ambient',
    ],
  },
  p_env_punch: {
    name: 'Punch',
    description: 'Extra volume boost at the start of sustain',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'envelope',
    tips: [
      'Higher values make the sound punchier and more impactful',
    ],
  },
  p_env_decay: {
    name: 'Decay',
    description: 'Time for sound to fade out after sustain',
    range: { min: 0, max: 1 },
    default: 0.4,
    signed: false,
    category: 'envelope',
    tips: [
      'Short: Snappy, percussive',
      'Long: Fading, atmospheric',
    ],
  },
  p_base_freq: {
    name: 'Base Frequency',
    description: 'Starting pitch of the sound',
    range: { min: 0, max: 1 },
    default: 0.3,
    signed: false,
    category: 'frequency',
    tips: [
      'Lower (0.1-0.3): Deep, bass sounds',
      'Mid (0.3-0.5): Standard pitch range',
      'Higher (0.5-0.8): High-pitched, sharp sounds',
      '"Make it deeper" → decrease this value',
      '"Make it higher" → increase this value',
    ],
  },
  p_freq_limit: {
    name: 'Frequency Limit',
    description: 'Minimum frequency cutoff - sound stops if it goes below this',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'frequency',
  },
  p_freq_ramp: {
    name: 'Frequency Slide',
    description: 'How the pitch changes over time',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'frequency',
    tips: [
      'Negative: Pitch slides down (lasers, hits)',
      'Positive: Pitch slides up (power-ups, jumps)',
      '"Falling sound" → use negative value',
      '"Rising sound" → use positive value',
    ],
  },
  p_freq_dramp: {
    name: 'Delta Slide',
    description: 'Acceleration of the frequency slide',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'frequency',
  },
  p_vib_strength: {
    name: 'Vibrato Depth',
    description: 'Amount of pitch wobble',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'vibrato',
    tips: [
      'Adds warble/wobble effect to the sound',
    ],
  },
  p_vib_speed: {
    name: 'Vibrato Speed',
    description: 'Speed of the vibrato effect',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'vibrato',
  },
  p_arp_mod: {
    name: 'Arpeggio Amount',
    description: 'Pitch change for arpeggio effect',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'arpeggio',
    tips: [
      'Creates a distinct pitch jump',
      'Positive: Jump up then back',
      'Negative: Jump down then back',
    ],
  },
  p_arp_speed: {
    name: 'Arpeggio Speed',
    description: 'How quickly the arpeggio change happens',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'arpeggio',
  },
  p_duty: {
    name: 'Square Duty',
    description: 'Duty cycle for square wave (affects tone)',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'duty',
    tips: [
      'Only affects square waves',
      '0.5 = 50% duty, standard square',
      'Lower values = thinner, buzzier sound',
    ],
  },
  p_duty_ramp: {
    name: 'Duty Sweep',
    description: 'How duty cycle changes over time',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'duty',
  },
  p_repeat_speed: {
    name: 'Repeat Speed',
    description: 'Rate at which the sound pattern repeats',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'repeat',
    tips: [
      'Creates a retriggering effect',
      'Higher values = faster repeats',
    ],
  },
  p_pha_offset: {
    name: 'Phaser Offset',
    description: 'Starting offset for phaser effect',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'phaser',
    tips: [
      'Adds a hollow, phasing quality',
    ],
  },
  p_pha_ramp: {
    name: 'Phaser Sweep',
    description: 'How the phaser offset changes over time',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'phaser',
  },
  p_lpf_freq: {
    name: 'Low-pass Cutoff',
    description: 'Frequency above which sound is filtered out',
    range: { min: 0, max: 1 },
    default: 1,
    signed: false,
    category: 'filter',
    tips: [
      '1 = No filtering',
      'Lower values = muffled, darker sound',
      '"Make it muffled" → decrease this',
    ],
  },
  p_lpf_ramp: {
    name: 'Low-pass Sweep',
    description: 'How the low-pass cutoff changes over time',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'filter',
  },
  p_lpf_resonance: {
    name: 'Low-pass Resonance',
    description: 'Emphasis at the cutoff frequency',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'filter',
  },
  p_hpf_freq: {
    name: 'High-pass Cutoff',
    description: 'Frequency below which sound is filtered out',
    range: { min: 0, max: 1 },
    default: 0,
    signed: false,
    category: 'filter',
    tips: [
      '0 = No filtering',
      'Higher values = tinnier, thinner sound',
      '"Remove bass" → increase this',
    ],
  },
  p_hpf_ramp: {
    name: 'High-pass Sweep',
    description: 'How the high-pass cutoff changes over time',
    range: { min: -1, max: 1 },
    default: 0,
    signed: true,
    category: 'filter',
  },
  sound_vol: {
    name: 'Volume',
    description: 'Master volume of the sound',
    range: { min: 0, max: 1 },
    default: 0.5,
    signed: false,
    category: 'master',
  },
  sample_rate: {
    name: 'Sample Rate',
    description: 'Audio sample rate in Hz',
    range: { min: 8000, max: 48000 },
    default: 44100,
    signed: false,
    category: 'master',
  },
  sample_size: {
    name: 'Bit Depth',
    description: 'Bits per sample (8 or 16)',
    range: { min: 8, max: 16 },
    default: 8,
    signed: false,
    category: 'master',
  },
};

/**
 * Semantic guidance for common modifications.
 */
export const MODIFICATION_GUIDE: Record<string, { description: string; params: Record<string, string> }> = {
  'make_deeper': {
    description: 'Lower the pitch',
    params: { p_base_freq: 'decrease by 0.1-0.2' },
  },
  'make_higher': {
    description: 'Raise the pitch',
    params: { p_base_freq: 'increase by 0.1-0.2' },
  },
  'make_longer': {
    description: 'Extend the duration',
    params: { p_env_sustain: 'increase', p_env_decay: 'increase' },
  },
  'make_shorter': {
    description: 'Shorten the duration',
    params: { p_env_sustain: 'decrease', p_env_decay: 'decrease' },
  },
  'make_punchier': {
    description: 'Add impact',
    params: { p_env_punch: 'increase to 0.3-0.6', p_env_attack: 'set to 0' },
  },
  'make_softer': {
    description: 'Reduce impact',
    params: { p_env_punch: 'decrease', p_env_attack: 'increase' },
  },
  'add_falling_pitch': {
    description: 'Pitch slides down',
    params: { p_freq_ramp: 'set negative (-0.2 to -0.5)' },
  },
  'add_rising_pitch': {
    description: 'Pitch slides up',
    params: { p_freq_ramp: 'set positive (0.1 to 0.3)' },
  },
  'make_muffled': {
    description: 'Darker, muted sound',
    params: { p_lpf_freq: 'decrease to 0.3-0.6' },
  },
  'make_sharper': {
    description: 'Brighter, crisper sound',
    params: { p_lpf_freq: 'increase towards 1', p_hpf_freq: 'increase slightly' },
  },
  'add_wobble': {
    description: 'Add vibrato effect',
    params: { p_vib_strength: 'set to 0.2-0.5', p_vib_speed: 'set to 0.2-0.5' },
  },
};

/**
 * Get the full parameter schema for AI parsing.
 */
export function getParameterSchema(): object {
  return {
    parameters: Object.entries(PARAMETER_INFO).map(([key, info]) => ({
      key,
      ...info,
    })),
    modification_guide: MODIFICATION_GUIDE,
    wave_types: {
      0: 'Square - Classic retro, blips, UI',
      1: 'Sawtooth - Bright, edgy, lasers',
      2: 'Sine - Pure tone, soft',
      3: 'Noise - Explosions, hits',
      4: 'Triangle - Softer than square',
      5: 'PinkNoise - Warmer noise',
      6: 'Tan - Harsh, digital',
      7: 'Whistle - Smooth, airy',
    },
  };
}
