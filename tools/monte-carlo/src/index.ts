/**
 * Monte Carlo Combat Simulator for Pits of Despair
 *
 * Entry point for the CLI application.
 */

import { createProgram } from './cli/commands.js';

const program = createProgram();
program.parse(process.argv);
