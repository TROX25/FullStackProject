# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commit behavior

When the user types "commit":
1. Stage all changed files
2. Generate a commit message following the format in `.github/copilot-instructions.md`: `<type>: <short description>` — one line, no body
3. Commit and push to the current branch on both `origin` (Bitbucket) and `github` (GitHub)

