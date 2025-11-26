/**
 * CLI command definitions using Commander.
 */

import { Command } from 'commander';
import { generateFromPreset, generateFromParams, generateFromPresetWithOverrides } from '../core/generator.js';
import { writeWavFile } from '../core/wav-writer.js';
import {
  createSession,
  loadSession,
  updateSession,
  deleteSession,
  listSessions,
  getSessionWavPath,
  getSessionParamsPath,
  getRelativePath,
} from '../core/session.js';
import {
  outputJson,
  createGenerateResponse,
  createPlayResponse,
  createAcceptResponse,
  createErrorResponse,
  createPresetsListResponse,
  createCategoriesListResponse,
  createSessionsListResponse,
  createSessionInfoResponse,
  ErrorCodes,
} from '../output/json-reporter.js';
import { PRESET_INFO, CATEGORY_INFO } from '../data/presets.js';
import { SOUND_CATEGORIES, JSFXR_PRESETS } from '../data/types.js';
import type { SoundCategory, JsfxrPreset, PartialJsfxrParams } from '../data/types.js';

/**
 * Validate a preset name.
 */
function isValidPreset(preset: string): preset is JsfxrPreset {
  return JSFXR_PRESETS.includes(preset as JsfxrPreset);
}

/**
 * Validate a category name.
 */
function isValidCategory(category: string): category is SoundCategory {
  return SOUND_CATEGORIES.includes(category as SoundCategory);
}

/**
 * Parse JSON params from CLI argument.
 */
function parseParams(paramsJson: string | undefined): PartialJsfxrParams | null {
  if (!paramsJson) return null;
  try {
    return JSON.parse(paramsJson) as PartialJsfxrParams;
  } catch {
    return null;
  }
}

/**
 * Create the CLI program.
 */
export function createProgram(): Command {
  const program = new Command();

  program
    .name('sound-generator')
    .description('AI-driven 8-bit sound effect generator for Pits of Despair')
    .version('1.0.0');

  // Generate command
  program
    .command('generate <name>')
    .description('Generate a new sound effect')
    .option('-p, --preset <preset>', 'jsfxr preset to use (default: random)')
    .option('-c, --category <category>', 'Sound category (default: Combat)')
    .option('--params <json>', 'JSON parameters to override')
    .option('--play', 'Play sound after generating')
    .action(async (name: string, options: { preset?: string; category?: string; params?: string; play?: boolean }) => {
      try {
        // Validate category
        const category: SoundCategory = options.category
          ? (isValidCategory(options.category) ? options.category : 'Combat')
          : 'Combat';

        if (options.category && !isValidCategory(options.category)) {
          outputJson(createErrorResponse(
            ErrorCodes.INVALID_CATEGORY,
            `Invalid category '${options.category}'. Use 'list categories' to see valid options.`
          ));
          return;
        }

        // Validate preset if provided
        const preset: JsfxrPreset = options.preset
          ? (isValidPreset(options.preset) ? options.preset : 'random')
          : 'random';

        if (options.preset && !isValidPreset(options.preset)) {
          outputJson(createErrorResponse(
            ErrorCodes.INVALID_PRESET,
            `Invalid preset '${options.preset}'. Use 'list presets' to see valid options.`
          ));
          return;
        }

        // Parse params override
        const paramsOverride = parseParams(options.params);
        if (options.params && !paramsOverride) {
          outputJson(createErrorResponse(
            ErrorCodes.INVALID_PARAMS,
            'Invalid JSON in --params argument'
          ));
          return;
        }

        // Generate the sound
        const sound = paramsOverride
          ? await generateFromPresetWithOverrides(preset, paramsOverride)
          : await generateFromPreset(preset);

        // Create session
        const session = await createSession(name, category, sound.params, preset);

        // Write WAV file
        const wavPath = getSessionWavPath(session.id, name);
        await writeWavFile(sound, wavPath);

        // Output response
        outputJson(createGenerateResponse(
          session.id,
          name,
          category,
          session.iterations,
          sound.params,
          sound.durationMs,
          getRelativePath(wavPath),
          getRelativePath(getSessionParamsPath(session.id)),
          preset
        ));

        // Play if requested
        if (options.play) {
          await playSound(wavPath);
        }
      } catch (error) {
        outputJson(createErrorResponse(
          ErrorCodes.GENERATION_FAILED,
          error instanceof Error ? error.message : 'Unknown error during generation'
        ));
      }
    });

  // Iterate command
  program
    .command('iterate <session-id>')
    .description('Modify an existing sound in a session')
    .option('--params <json>', 'JSON parameters to modify')
    .option('--play', 'Play sound after iterating')
    .action(async (sessionId: string, options: { params?: string; play?: boolean }) => {
      try {
        // Load existing session
        const session = await loadSession(sessionId);
        if (!session) {
          outputJson(createErrorResponse(
            ErrorCodes.SESSION_NOT_FOUND,
            `Session '${sessionId}' not found`
          ));
          return;
        }

        // Parse params
        const paramsOverride = parseParams(options.params);
        if (options.params && !paramsOverride) {
          outputJson(createErrorResponse(
            ErrorCodes.INVALID_PARAMS,
            'Invalid JSON in --params argument'
          ));
          return;
        }

        // Merge params
        const newParams = {
          ...session.params,
          ...paramsOverride,
        };

        // Generate new sound
        const sound = await generateFromParams(newParams);

        // Update session
        const updatedSession = await updateSession(sessionId, sound.params);
        if (!updatedSession) {
          outputJson(createErrorResponse(
            ErrorCodes.SESSION_NOT_FOUND,
            `Failed to update session '${sessionId}'`
          ));
          return;
        }

        // Write new WAV file
        const wavPath = getSessionWavPath(sessionId, session.name);
        await writeWavFile(sound, wavPath);

        // Output response
        outputJson(createGenerateResponse(
          sessionId,
          session.name,
          session.category,
          updatedSession.iterations,
          sound.params,
          sound.durationMs,
          getRelativePath(wavPath),
          getRelativePath(getSessionParamsPath(sessionId)),
          session.preset
        ));

        // Play if requested
        if (options.play) {
          await playSound(wavPath);
        }
      } catch (error) {
        outputJson(createErrorResponse(
          ErrorCodes.GENERATION_FAILED,
          error instanceof Error ? error.message : 'Unknown error during iteration'
        ));
      }
    });

  // Play command
  program
    .command('play <session-id>')
    .description('Play the current working sound')
    .action(async (sessionId: string) => {
      try {
        const session = await loadSession(sessionId);
        if (!session) {
          outputJson(createErrorResponse(
            ErrorCodes.SESSION_NOT_FOUND,
            `Session '${sessionId}' not found`
          ));
          return;
        }

        const wavPath = getSessionWavPath(sessionId, session.name);

        // Calculate duration from params
        const sound = await generateFromParams(session.params);

        await playSound(wavPath);

        outputJson(createPlayResponse(
          sessionId,
          getRelativePath(wavPath),
          sound.durationMs
        ));
      } catch (error) {
        outputJson(createErrorResponse(
          ErrorCodes.PLAYBACK_FAILED,
          error instanceof Error ? error.message : 'Unknown error during playback'
        ));
      }
    });

  // Accept command
  program
    .command('accept <session-id>')
    .description('Accept and finalize the sound')
    .option('--force', 'Overwrite existing file')
    .action(async (sessionId: string, options: { force?: boolean }) => {
      try {
        const session = await loadSession(sessionId);
        if (!session) {
          outputJson(createErrorResponse(
            ErrorCodes.SESSION_NOT_FOUND,
            `Session '${sessionId}' not found`
          ));
          return;
        }

        // Determine final path
        const { copyFile, mkdir, access } = await import('fs/promises');
        const { join, dirname } = await import('path');
        const { fileURLToPath } = await import('url');

        const __dirname = dirname(fileURLToPath(import.meta.url));
        const projectRoot = join(__dirname, '..', '..', '..', '..');
        const finalDir = join(projectRoot, 'Resources', 'Audio', 'SoundEffects', session.category);
        const finalPath = join(finalDir, `${session.name}.wav`);

        // Check if file exists
        if (!options.force) {
          try {
            await access(finalPath);
            outputJson(createErrorResponse(
              ErrorCodes.FILE_EXISTS,
              `File already exists: ${getRelativePath(finalPath)}. Use --force to overwrite.`
            ));
            return;
          } catch {
            // File doesn't exist, continue
          }
        }

        // Create directory and copy file
        await mkdir(finalDir, { recursive: true });
        const sourcePath = getSessionWavPath(sessionId, session.name);
        await copyFile(sourcePath, finalPath);

        // Clean up session
        await deleteSession(sessionId);

        outputJson(createAcceptResponse(
          sessionId,
          `Resources/Audio/SoundEffects/${session.category}/${session.name}.wav`
        ));
      } catch (error) {
        outputJson(createErrorResponse(
          ErrorCodes.GENERATION_FAILED,
          error instanceof Error ? error.message : 'Unknown error during accept'
        ));
      }
    });

  // List command
  program
    .command('list <type>')
    .description('List presets, categories, or sessions')
    .action(async (type: string) => {
      switch (type) {
        case 'presets':
        case 'preset':
          outputJson(createPresetsListResponse([...PRESET_INFO]));
          break;
        case 'categories':
        case 'category':
          outputJson(createCategoriesListResponse(CATEGORY_INFO));
          break;
        case 'sessions':
        case 'session':
          const sessions = await listSessions();
          outputJson(createSessionsListResponse(sessions));
          break;
        default:
          outputJson(createErrorResponse(
            ErrorCodes.INVALID_PARAMS,
            `Unknown list type: '${type}'. Use 'presets', 'categories', or 'sessions'.`
          ));
      }
    });

  // Info command
  program
    .command('info <session-id>')
    .description('Show session details')
    .action(async (sessionId: string) => {
      const session = await loadSession(sessionId);
      if (!session) {
        outputJson(createErrorResponse(
          ErrorCodes.SESSION_NOT_FOUND,
          `Session '${sessionId}' not found`
        ));
        return;
      }

      outputJson(createSessionInfoResponse(
        session.id,
        session.name,
        session.category,
        session.iterations,
        session.params,
        session.createdAt,
        session.updatedAt,
        session.preset
      ));
    });

  // Schema command
  program
    .command('schema')
    .description('Output JSON schema for jsfxr parameters')
    .action(async () => {
      const { getParameterSchema } = await import('../data/schemas.js');
      outputJson({
        status: 'success',
        schema: getParameterSchema(),
      });
    });

  return program;
}

/**
 * Play a WAV file using node-wav-player.
 */
async function playSound(wavPath: string): Promise<void> {
  try {
    // Dynamic import to handle the CommonJS module
    const { createRequire } = await import('module');
    const require = createRequire(import.meta.url);
    const player = require('node-wav-player');
    await player.play({ path: wavPath });
  } catch (error) {
    // Silently fail on playback errors - not all systems support audio
    console.error('Playback not available:', error instanceof Error ? error.message : 'Unknown error');
  }
}
