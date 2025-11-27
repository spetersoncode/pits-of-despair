/**
 * Session management for sound iteration workflow.
 */

import { readFile, writeFile, mkdir, readdir, rm } from 'fs/promises';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { customAlphabet } from 'nanoid';
import type { Session, SessionMetadata, SoundCategory, JsfxrParams, JsfxrPreset } from '../data/types.js';

const __dirname = dirname(fileURLToPath(import.meta.url));

const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
const nanoid = customAlphabet(alphabet, 8);
const WORKING_DIR = join(__dirname, '..', '..', 'working');

/**
 * Generate a unique session ID.
 */
export function generateSessionId(): string {
  return nanoid();
}

/**
 * Get the directory path for a session.
 */
export function getSessionDir(sessionId: string): string {
  return join(WORKING_DIR, sessionId);
}

/**
 * Get the WAV file path for a session.
 */
export function getSessionWavPath(sessionId: string, name: string): string {
  return join(getSessionDir(sessionId), `${name}.wav`);
}

/**
 * Get the params.json path for a session.
 */
export function getSessionParamsPath(sessionId: string): string {
  return join(getSessionDir(sessionId), 'params.json');
}

/**
 * Get the session.json path for a session.
 */
export function getSessionMetadataPath(sessionId: string): string {
  return join(getSessionDir(sessionId), 'session.json');
}

/**
 * Create a new session.
 */
export async function createSession(
  name: string,
  category: SoundCategory,
  params: JsfxrParams,
  preset?: JsfxrPreset
): Promise<Session> {
  const id = generateSessionId();
  const now = new Date().toISOString();

  const session: Session = {
    id,
    name,
    category,
    preset,
    iterations: 1,
    createdAt: now,
    updatedAt: now,
    params,
  };

  // Create session directory
  const sessionDir = getSessionDir(id);
  await mkdir(sessionDir, { recursive: true });

  // Save session metadata
  await saveSessionMetadata(session);

  // Save params
  await saveSessionParams(id, params);

  return session;
}

/**
 * Save session metadata to session.json.
 */
async function saveSessionMetadata(session: Session): Promise<void> {
  const metadata: SessionMetadata = {
    id: session.id,
    name: session.name,
    category: session.category,
    preset: session.preset,
    iterations: session.iterations,
    createdAt: session.createdAt,
    updatedAt: session.updatedAt,
  };

  const path = getSessionMetadataPath(session.id);
  await writeFile(path, JSON.stringify(metadata, null, 2));
}

/**
 * Save params to params.json.
 */
async function saveSessionParams(sessionId: string, params: JsfxrParams): Promise<void> {
  const path = getSessionParamsPath(sessionId);
  await writeFile(path, JSON.stringify(params, null, 2));
}

/**
 * Load a session by ID.
 */
export async function loadSession(sessionId: string): Promise<Session | null> {
  try {
    const metadataPath = getSessionMetadataPath(sessionId);
    const paramsPath = getSessionParamsPath(sessionId);

    const [metadataJson, paramsJson] = await Promise.all([
      readFile(metadataPath, 'utf-8'),
      readFile(paramsPath, 'utf-8'),
    ]);

    const metadata = JSON.parse(metadataJson) as SessionMetadata;
    const params = JSON.parse(paramsJson) as JsfxrParams;

    return {
      ...metadata,
      params,
    };
  } catch {
    return null;
  }
}

/**
 * Update a session with new params (for iteration).
 */
export async function updateSession(
  sessionId: string,
  newParams: JsfxrParams
): Promise<Session | null> {
  const session = await loadSession(sessionId);
  if (!session) {
    return null;
  }

  session.params = newParams;
  session.iterations += 1;
  session.updatedAt = new Date().toISOString();

  await saveSessionMetadata(session);
  await saveSessionParams(sessionId, newParams);

  return session;
}

/**
 * Delete a session and its working files.
 */
export async function deleteSession(sessionId: string): Promise<boolean> {
  try {
    const sessionDir = getSessionDir(sessionId);
    await rm(sessionDir, { recursive: true, force: true });
    return true;
  } catch {
    return false;
  }
}

/**
 * List all active sessions.
 */
export async function listSessions(): Promise<SessionMetadata[]> {
  try {
    await mkdir(WORKING_DIR, { recursive: true });
    const entries = await readdir(WORKING_DIR, { withFileTypes: true });
    const sessions: SessionMetadata[] = [];

    for (const entry of entries) {
      if (entry.isDirectory()) {
        const session = await loadSession(entry.name);
        if (session) {
          const { params: _params, ...metadata } = session;
          sessions.push(metadata);
        }
      }
    }

    // Sort by updatedAt descending (most recent first)
    sessions.sort((a, b) =>
      new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    );

    return sessions;
  } catch {
    return [];
  }
}

/**
 * Check if a session exists.
 */
export async function sessionExists(sessionId: string): Promise<boolean> {
  const session = await loadSession(sessionId);
  return session !== null;
}

/**
 * Get the relative path from project root for a session file.
 */
export function getRelativePath(absolutePath: string): string {
  // Convert to forward slashes for consistency
  const normalized = absolutePath.replace(/\\/g, '/');
  const workingIndex = normalized.indexOf('tools/sound-generator/');
  if (workingIndex !== -1) {
    return normalized.substring(workingIndex);
  }
  return normalized;
}
