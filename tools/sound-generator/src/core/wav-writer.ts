/**
 * WAV file writer for generated sounds.
 */

import { writeFile, mkdir } from 'fs/promises';
import { dirname } from 'path';
import type { GeneratedSound } from './generator.js';

/**
 * WAV file header structure.
 */
interface WavHeader {
  chunkId: number[];      // "RIFF"
  chunkSize: number;      // File size - 8
  format: number[];       // "WAVE"
  subChunk1Id: number[];  // "fmt "
  subChunk1Size: number;  // 16 for PCM
  audioFormat: number;    // 1 for PCM
  numChannels: number;    // 1 = mono
  sampleRate: number;     // e.g., 44100
  byteRate: number;       // sampleRate * numChannels * bitsPerSample/8
  blockAlign: number;     // numChannels * bitsPerSample/8
  bitsPerSample: number;  // 8 or 16
  subChunk2Id: number[];  // "data"
  subChunk2Size: number;  // data size
}

/**
 * Convert 32-bit unsigned int to little-endian byte array.
 */
function u32ToArray(i: number): number[] {
  return [i & 0xff, (i >> 8) & 0xff, (i >> 16) & 0xff, (i >> 24) & 0xff];
}

/**
 * Convert 16-bit unsigned int to little-endian byte array.
 */
function u16ToArray(i: number): number[] {
  return [i & 0xff, (i >> 8) & 0xff];
}

/**
 * Build WAV file bytes from audio data.
 */
function buildWavBytes(
  data: number[],
  sampleRate: number,
  bitsPerSample: number
): Uint8Array {
  const numChannels = 1; // Mono
  const bytesPerSample = bitsPerSample / 8;

  const header: WavHeader = {
    chunkId: [0x52, 0x49, 0x46, 0x46], // "RIFF"
    chunkSize: 0,
    format: [0x57, 0x41, 0x56, 0x45], // "WAVE"
    subChunk1Id: [0x66, 0x6d, 0x74, 0x20], // "fmt "
    subChunk1Size: 16,
    audioFormat: 1, // PCM
    numChannels,
    sampleRate,
    byteRate: sampleRate * numChannels * bytesPerSample,
    blockAlign: numChannels * bytesPerSample,
    bitsPerSample,
    subChunk2Id: [0x64, 0x61, 0x74, 0x61], // "data"
    subChunk2Size: data.length * bytesPerSample,
  };

  header.chunkSize = 36 + header.subChunk2Size;

  // Convert data to bytes based on bit depth
  let dataBytes: number[];
  if (bitsPerSample === 8) {
    // 8-bit is unsigned (0-255)
    dataBytes = data;
  } else {
    // 16-bit is signed, little-endian
    dataBytes = [];
    for (const sample of data) {
      dataBytes.push(sample & 0xff);
      dataBytes.push((sample >> 8) & 0xff);
    }
  }

  // Build final WAV byte array
  const wavBytes = [
    ...header.chunkId,
    ...u32ToArray(header.chunkSize),
    ...header.format,
    ...header.subChunk1Id,
    ...u32ToArray(header.subChunk1Size),
    ...u16ToArray(header.audioFormat),
    ...u16ToArray(header.numChannels),
    ...u32ToArray(header.sampleRate),
    ...u32ToArray(header.byteRate),
    ...u16ToArray(header.blockAlign),
    ...u16ToArray(header.bitsPerSample),
    ...header.subChunk2Id,
    ...u32ToArray(header.subChunk2Size),
    ...dataBytes,
  ];

  return new Uint8Array(wavBytes);
}

/**
 * Write a generated sound to a WAV file.
 */
export async function writeWavFile(
  sound: GeneratedSound,
  filePath: string
): Promise<void> {
  // Ensure directory exists
  const dir = dirname(filePath);
  await mkdir(dir, { recursive: true });

  // Build WAV bytes
  const wavBytes = buildWavBytes(
    sound.buffer,
    sound.sampleRate,
    sound.bitsPerSample
  );

  // Write file
  await writeFile(filePath, wavBytes);
}

/**
 * Get WAV bytes without writing to file.
 */
export function getWavBytes(sound: GeneratedSound): Uint8Array {
  return buildWavBytes(sound.buffer, sound.sampleRate, sound.bitsPerSample);
}
