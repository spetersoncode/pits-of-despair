/**
 * Sound Generator CLI for Pits of Despair
 *
 * AI-driven 8-bit sound effect generation using jsfxr.
 */

import { createProgram } from './cli/commands.js';

const program = createProgram();
program.parse(process.argv);
