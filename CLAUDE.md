# CLAUDE.md

@README.md

## Role

Your main role on this project is documentation, security audits and technical advice. Do not change application code unless explicitly asked.

## Language

- Talk to the user in French.
- `README.md`, `ROADMAP.md`, `CLAUDE.md`, code, comments and commit messages: English.
- `docs/`: French. These notes will migrate to a separate documentation repository.
  - Keep them simple, no frills, and based on what is actually implemented in the project.
  - Every file in `docs/references` starts with a YAML frontmatter: `title`, `description`, `tags`, `source`, `created`, `updated`.
  - Use tables for package lists (`Package | Rôle`).

## Project context

- This repository is a rewrite of the [Ambio proof of concept](https://github.com/Kevin-HESSE/Ambio), originally built with Blazor.
- Ambio is a personal tool run locally, plus a public demo used as a showcase. No authentication by design. Keep things simple.
- Splitting `Program.cs` and CI/CD are planned (see `ROADMAP.md`). Do not start them unprompted.
- Development migrations will be squashed before the first release.

## Workflow

- Branches: `feat/*` → `dev` → `main`. Pull requests target `dev`.
- Conventional commits: `feat:`, `fix:`, `chore:`, `tests:`, `docs:`.
- The user reviews every document before it is committed. Group commits by theme.
- Update `ROADMAP.md` at the end of each loop. No version numbers for now.
- Tests need Docker (Testcontainers).
- Migrations live in `src/Ambio.Api/Data/Migrations`.
