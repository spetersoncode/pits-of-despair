/**
 * jsfxr wrapper for sound generation.
 */

import type { JsfxrParams, JsfxrPreset, PartialJsfxrParams } from '../data/types.js';

// jsfxr module interface based on actual exports
interface JsfxrModule {
  sfxr: {
    generate: (preset: string, options?: Record<string, number>) => Record<string, number>;
    toBuffer: (params: Record<string, number>) => number[];
  };
  Params: new () => Record<string, unknown>;
  SoundEffect: new (params: Record<string, number>) => {
    getRawBuffer: () => { buffer: number[]; normalized: number[]; clipped: number };
    sampleRate: number;
    bitsPerChannel: number;
  };
}

let jsfxrModule: JsfxrModule | null = null;

async function loadJsfxr(): Promise<JsfxrModule> {
  if (!jsfxrModule) {
    // Use createRequire for CommonJS module
    const { createRequire } = await import('module');
    const require = createRequire(import.meta.url);
    jsfxrModule = require('jsfxr') as JsfxrModule;
  }
  return jsfxrModule;
}

/**
 * Default parameters for a sound.
 */
export function getDefaultParams(): JsfxrParams {
  return {
    wave_type: 0, // Square
    p_env_attack: 0,
    p_env_sustain: 0.3,
    p_env_punch: 0,
    p_env_decay: 0.4,
    p_base_freq: 0.3,
    p_freq_limit: 0,
    p_freq_ramp: 0,
    p_freq_dramp: 0,
    p_vib_strength: 0,
    p_vib_speed: 0,
    p_arp_mod: 0,
    p_arp_speed: 0,
    p_duty: 0,
    p_duty_ramp: 0,
    p_repeat_speed: 0,
    p_pha_offset: 0,
    p_pha_ramp: 0,
    p_lpf_freq: 1,
    p_lpf_ramp: 0,
    p_lpf_resonance: 0,
    p_hpf_freq: 0,
    p_hpf_ramp: 0,
    sound_vol: 0.5,
    sample_rate: 44100,
    sample_size: 8,
  };
}

/**
 * Result of sound generation.
 */
export interface GeneratedSound {
  buffer: number[];
  params: JsfxrParams;
  sampleRate: number;
  bitsPerSample: number;
  durationMs: number;
}

/**
 * Generate a sound from a preset.
 */
export async function generateFromPreset(preset: JsfxrPreset): Promise<GeneratedSound> {
  const jsfxr = await loadJsfxr();

  // Use sfxr.generate to create params from preset
  const params = jsfxr.sfxr.generate(preset, {
    sound_vol: 0.5,
    sample_rate: 44100,
    sample_size: 8,
  });

  // Use sfxr.toBuffer to generate audio
  const buffer = jsfxr.sfxr.toBuffer(params);

  const fullParams = { ...getDefaultParams(), ...params } as JsfxrParams;
  const durationMs = calculateDuration(buffer.length, fullParams.sample_rate);

  return {
    buffer: Array.from(buffer),
    params: fullParams,
    sampleRate: fullParams.sample_rate,
    bitsPerSample: fullParams.sample_size,
    durationMs,
  };
}

/**
 * Generate a sound from explicit parameters.
 */
export async function generateFromParams(params: PartialJsfxrParams): Promise<GeneratedSound> {
  const jsfxr = await loadJsfxr();

  const fullParams: JsfxrParams = {
    ...getDefaultParams(),
    ...params,
  };

  const buffer = jsfxr.sfxr.toBuffer(fullParams as Record<string, number>);
  const durationMs = calculateDuration(buffer.length, fullParams.sample_rate);

  return {
    buffer: Array.from(buffer),
    params: fullParams,
    sampleRate: fullParams.sample_rate,
    bitsPerSample: fullParams.sample_size,
    durationMs,
  };
}

/**
 * Generate a sound from a preset, then apply parameter overrides.
 */
export async function generateFromPresetWithOverrides(
  preset: JsfxrPreset,
  overrides: PartialJsfxrParams
): Promise<GeneratedSound> {
  const jsfxr = await loadJsfxr();

  // Generate base params from preset
  const baseParams = jsfxr.sfxr.generate(preset, {
    sound_vol: 0.5,
    sample_rate: 44100,
    sample_size: 8,
  });

  const fullParams: JsfxrParams = {
    ...getDefaultParams(),
    ...baseParams,
    ...overrides,
  } as JsfxrParams;

  const buffer = jsfxr.sfxr.toBuffer(fullParams as Record<string, number>);
  const durationMs = calculateDuration(buffer.length, fullParams.sample_rate);

  return {
    buffer: Array.from(buffer),
    params: fullParams,
    sampleRate: fullParams.sample_rate,
    bitsPerSample: fullParams.sample_size,
    durationMs,
  };
}

/**
 * Calculate duration in milliseconds from buffer length and sample rate.
 */
function calculateDuration(bufferLength: number, sampleRate: number): number {
  return Math.round((bufferLength / sampleRate) * 1000);
}
