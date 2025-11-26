/**
 * Type definitions for the Sound Generator CLI.
 */

// =============================================================================
// Sound Categories
// =============================================================================

export type SoundCategory =
  | 'Combat'
  | 'Magic'
  | 'Items'
  | 'UI'
  | 'Environment'
  | 'Deaths'
  | 'Player'
  | 'Enemy';

export const SOUND_CATEGORIES: readonly SoundCategory[] = [
  'Combat',
  'Magic',
  'Items',
  'UI',
  'Environment',
  'Deaths',
  'Player',
  'Enemy',
] as const;

// =============================================================================
// jsfxr Wave Types
// =============================================================================

export type WaveType =
  | 0  // Square
  | 1  // Sawtooth
  | 2  // Sine
  | 3  // Noise
  | 4  // Triangle
  | 5  // Pink Noise
  | 6  // Tan
  | 7; // Whistle

export const WAVE_TYPES = {
  Square: 0,
  Sawtooth: 1,
  Sine: 2,
  Noise: 3,
  Triangle: 4,
  PinkNoise: 5,
  Tan: 6,
  Whistle: 7,
} as const;

// =============================================================================
// jsfxr Preset Types
// =============================================================================

export type JsfxrPreset =
  | 'pickupCoin'
  | 'laserShoot'
  | 'explosion'
  | 'powerUp'
  | 'hitHurt'
  | 'jump'
  | 'blipSelect'
  | 'synth'
  | 'tone'
  | 'click'
  | 'random';

export const JSFXR_PRESETS: readonly JsfxrPreset[] = [
  'pickupCoin',
  'laserShoot',
  'explosion',
  'powerUp',
  'hitHurt',
  'jump',
  'blipSelect',
  'synth',
  'tone',
  'click',
  'random',
] as const;

// =============================================================================
// jsfxr Parameters
// =============================================================================

/**
 * Full jsfxr parameter set.
 * All parameters range from 0-1 unless otherwise noted.
 */
export interface JsfxrParams {
  // Wave
  wave_type: WaveType;

  // Envelope
  p_env_attack: number;   // Attack time (0-1)
  p_env_sustain: number;  // Sustain time (0-1)
  p_env_punch: number;    // Sustain punch (0-1)
  p_env_decay: number;    // Decay time (0-1)

  // Frequency
  p_base_freq: number;    // Start frequency (0-1)
  p_freq_limit: number;   // Min frequency cutoff (0-1)
  p_freq_ramp: number;    // Slide (-1 to 1)
  p_freq_dramp: number;   // Delta slide (-1 to 1)

  // Vibrato
  p_vib_strength: number; // Vibrato depth (0-1)
  p_vib_speed: number;    // Vibrato speed (0-1)

  // Arpeggiation
  p_arp_mod: number;      // Change amount (-1 to 1)
  p_arp_speed: number;    // Change speed (0-1)

  // Duty (Square wave only)
  p_duty: number;         // Square duty (0-1)
  p_duty_ramp: number;    // Duty sweep (-1 to 1)

  // Retrigger
  p_repeat_speed: number; // Repeat speed (0-1)

  // Phaser
  p_pha_offset: number;   // Phaser offset (-1 to 1)
  p_pha_ramp: number;     // Phaser sweep (-1 to 1)

  // Low-pass filter
  p_lpf_freq: number;     // LP filter cutoff (0-1)
  p_lpf_ramp: number;     // LP filter cutoff sweep (-1 to 1)
  p_lpf_resonance: number; // LP filter resonance (0-1)

  // High-pass filter
  p_hpf_freq: number;     // HP filter cutoff (0-1)
  p_hpf_ramp: number;     // HP filter cutoff sweep (-1 to 1)

  // Master
  sound_vol: number;      // Master volume (0-1)
  sample_rate: number;    // Sample rate (e.g., 44100)
  sample_size: number;    // Bits per sample (8 or 16)
}

/**
 * Partial params for iteration - only specify what you want to change.
 */
export type PartialJsfxrParams = Partial<JsfxrParams>;

// =============================================================================
// Session Types
// =============================================================================

/**
 * Session metadata stored in session.json.
 */
export interface SessionMetadata {
  id: string;
  name: string;
  category: SoundCategory;
  preset?: JsfxrPreset;
  iterations: number;
  createdAt: string;
  updatedAt: string;
}

/**
 * Full session data including parameters.
 */
export interface Session extends SessionMetadata {
  params: JsfxrParams;
}

// =============================================================================
// CLI Output Types
// =============================================================================

/**
 * Suggested next action for AI.
 */
export interface NextAction {
  action: string;
  command: string;
}

/**
 * Sound info in CLI output.
 */
export interface SoundInfo {
  preset?: JsfxrPreset;
  parameters: JsfxrParams;
  duration_ms: number;
}

/**
 * File paths in CLI output.
 */
export interface WorkingFiles {
  working_wav: string;
  working_params: string;
}

/**
 * Session info in CLI output.
 */
export interface SessionInfo {
  id: string;
  name: string;
  category: SoundCategory;
  iterations: number;
}

/**
 * Successful generate/iterate response.
 */
export interface GenerateResponse {
  status: 'success';
  session: SessionInfo;
  sound: SoundInfo;
  files: WorkingFiles;
  next_actions: NextAction[];
}

/**
 * Successful play response.
 */
export interface PlayResponse {
  status: 'success';
  action: 'play';
  session_id: string;
  file: string;
  duration_ms: number;
}

/**
 * Successful accept response.
 */
export interface AcceptResponse {
  status: 'success';
  action: 'accepted';
  session_id: string;
  final_path: string;
}

/**
 * Error response.
 */
export interface ErrorResponse {
  status: 'error';
  error: {
    code: string;
    message: string;
  };
}

/**
 * Union of all CLI responses.
 */
export type CliResponse =
  | GenerateResponse
  | PlayResponse
  | AcceptResponse
  | ErrorResponse;
