# Contributing

Thanks for considering a contribution to Innovayse Sheets.

## Getting started

1. Fork the repo and clone your fork.
2. Follow the setup instructions in [README.md](README.md) to get the backend and client running
   locally, including the SSO configuration described there.
3. Create a branch off `main` for your change.

## Making changes

- Keep pull requests focused on a single change — smaller PRs are easier to review.
- Match the existing code style in each part (`backend/` follows standard .NET conventions,
  `client/` follows the repo's TypeScript/Vue conventions).
- Add or update tests for any behavior change:
  - Backend: `dotnet test` from `backend/`
  - Client: `npm test` from `client/`

## Commit messages

Use a short imperative summary line, optionally followed by a blank line and more detail on the
*why* behind the change.

## Submitting a pull request

- Describe what the change does and why.
- Link any related issue.
- Make sure CI passes before requesting review.

## Reporting bugs

Open an issue with steps to reproduce, expected vs. actual behavior, and relevant environment
details (browser, OS, service version).
