/**
 * JSON output formatting for CLI responses.
 */

import type {
  GenerateResponse,
  PlayResponse,
  AcceptResponse,
  ErrorResponse,
  SessionInfo,
  SoundInfo,
  WorkingFiles,
  NextAction,
  SoundCategory,
  JsfxrParams,
  JsfxrPreset,
  SessionMetadata,
} from '../data/types.js';
import type { PresetInfo } from '../data/presets.js';

/**
 * Output JSON to stdout.
 */
export function outputJson(data: unknown): void {
  console.log(JSON.stringify(data, null, 2));
}

/**
 * Create a generate/iterate success response.
 */
export function createGenerateResponse(
  sessionId: string,
  name: string,
  category: SoundCategory,
  iterations: number,
  params: JsfxrParams,
  durationMs: number,
  workingWavPath: string,
  workingParamsPath: string,
  preset?: JsfxrPreset
): GenerateResponse {
  const session: SessionInfo = {
    id: sessionId,
    name,
    category,
    iterations,
  };

  const sound: SoundInfo = {
    preset,
    parameters: params,
    duration_ms: durationMs,
  };

  const files: WorkingFiles = {
    working_wav: workingWavPath,
    working_params: workingParamsPath,
  };

  const next_actions: NextAction[] = [
    {
      action: 'play',
      command: `npm run dev -- play ${sessionId}`,
    },
    {
      action: 'iterate',
      command: `npm run dev -- iterate ${sessionId} --params '{...}'`,
    },
    {
      action: 'accept',
      command: `npm run dev -- accept ${sessionId}`,
    },
  ];

  return {
    status: 'success',
    session,
    sound,
    files,
    next_actions,
  };
}

/**
 * Create a play success response.
 */
export function createPlayResponse(
  sessionId: string,
  filePath: string,
  durationMs: number
): PlayResponse {
  return {
    status: 'success',
    action: 'play',
    session_id: sessionId,
    file: filePath,
    duration_ms: durationMs,
  };
}

/**
 * Create an accept success response.
 */
export function createAcceptResponse(
  sessionId: string,
  finalPath: string
): AcceptResponse {
  return {
    status: 'success',
    action: 'accepted',
    session_id: sessionId,
    final_path: finalPath,
  };
}

/**
 * Create an error response.
 */
export function createErrorResponse(
  code: string,
  message: string
): ErrorResponse {
  return {
    status: 'error',
    error: {
      code,
      message,
    },
  };
}

/**
 * Common error codes.
 */
export const ErrorCodes = {
  SESSION_NOT_FOUND: 'SESSION_NOT_FOUND',
  INVALID_PRESET: 'INVALID_PRESET',
  INVALID_CATEGORY: 'INVALID_CATEGORY',
  INVALID_PARAMS: 'INVALID_PARAMS',
  FILE_EXISTS: 'FILE_EXISTS',
  GENERATION_FAILED: 'GENERATION_FAILED',
  PLAYBACK_FAILED: 'PLAYBACK_FAILED',
  NOT_IMPLEMENTED: 'NOT_IMPLEMENTED',
} as const;

/**
 * Create a list presets response.
 */
export function createPresetsListResponse(presets: PresetInfo[]): object {
  return {
    status: 'success',
    presets: presets.map((p) => ({
      id: p.id,
      name: p.name,
      description: p.description,
      typical_uses: p.typicalUses,
      suggested_categories: p.suggestedCategories,
    })),
  };
}

/**
 * Create a list categories response.
 */
export function createCategoriesListResponse(
  categories: Record<SoundCategory, string>
): object {
  return {
    status: 'success',
    categories: Object.entries(categories).map(([id, description]) => ({
      id,
      description,
    })),
  };
}

/**
 * Create a list sessions response.
 */
export function createSessionsListResponse(sessions: SessionMetadata[]): object {
  return {
    status: 'success',
    sessions: sessions.map((s) => ({
      id: s.id,
      name: s.name,
      category: s.category,
      preset: s.preset,
      iterations: s.iterations,
      created_at: s.createdAt,
      updated_at: s.updatedAt,
    })),
  };
}

/**
 * Create a session info response.
 */
export function createSessionInfoResponse(
  sessionId: string,
  name: string,
  category: SoundCategory,
  iterations: number,
  params: JsfxrParams,
  createdAt: string,
  updatedAt: string,
  preset?: JsfxrPreset
): object {
  return {
    status: 'success',
    session: {
      id: sessionId,
      name,
      category,
      preset,
      iterations,
      created_at: createdAt,
      updated_at: updatedAt,
      parameters: params,
    },
  };
}
