# Project Agent Instructions (ModdingAPI)

## Scope and Source of Truth
- Primary codebase: `/codex/ModdingAPI`.
- Main game analysis source: `/codex/hollow_knight_analysis`.
- Hollow Knight patch target for this port effort: `1.5.12459` unless the user explicitly redirects.
- The historical baseline in this repo is `1.5.78.11833`; treat differences from that patch as expected porting work.

## OS Context and Runtime Target (Required)
- Development/agent environment is Linux (`Ubuntu 24.04`).
- Hollow Knight may be installed and run through Steam on either Linux or Windows.
- Any disk persistence, path handling, file naming, log paths, and OS-specific behavior must be portable across Linux and Windows unless the user explicitly narrows scope.
- Do not hardcode Linux-only or Windows-only persistence assumptions for runtime data consumed by the API or mods.
- When runtime behavior differs by OS, prefer platform detection and correct native conventions over path-string heuristics.

## Project Goal
- This repo is for the Hollow Knight Modding API/loader itself.
- Current execution focus is porting the API from game version `1.5.78.11833` to `1.5.12459`.
- Before deeper patch-port work, prioritize:
  - understanding existing loader/bootstrap behavior,
  - preserving current intended behavior,
  - enabling diagnostics,
  - gathering runtime evidence,
  - then patching breakages in the smallest defensible increments.

## Working Style
- Keep patches concise and behavior-scoped.
- Prefer minimal-risk changes over wide refactors during porting.
- Preserve established API behavior unless the user explicitly asks for behavior changes.
- If uncertain, add or improve diagnostics and validation steps instead of making speculative compatibility changes.

## Build and Validation
- Typical local build flow depends on a local `Vanilla/` managed directory for the target game patch.
- Prefer commands that keep SDK/NuGet state out of the home directory in this environment, e.g.:
  - `DOTNET_CLI_HOME=/tmp/dotnet_home NUGET_PACKAGES=/tmp/nuget dotnet restore /codex/ModdingAPI/HollowKnight.Modding.API.sln`
  - `DOTNET_CLI_HOME=/tmp/dotnet_home NUGET_PACKAGES=/tmp/nuget dotnet build /codex/ModdingAPI/PrePatcher/PrePatcher.csproj -p:Configuration=Release`
  - `DOTNET_CLI_HOME=/tmp/dotnet_home NUGET_PACKAGES=/tmp/nuget dotnet msbuild /codex/ModdingAPI/Assembly-CSharp/Assembly-CSharp.csproj /t:Compile /p:Configuration=Release`
- If full post-build execution requires additional runtime tooling or local game installation, report that explicitly.
- When reporting build results, include the primary blocking error and whether the result is compile-time, post-build, or runtime.

## Issue Tracking and Milestones (Required)
- Use GitHub issues on the fork repo `jhearom/api` for implementation tracking.
- For this `1.5.12459` port effort, use issue `#1` as the main execution log unless the user creates or names a different issue.
- Milestones, material discoveries, validation results, blockers, and plan adjustments must be logged to the issue as work progresses.
- Keep issue updates concise, operational, and sufficient for resume without chat history.

## GitHub Comment Formatting (Required)
- When posting or editing GitHub issue comments via `gh`, use real multiline Markdown bodies.
- Do not pass escaped newline sequences in inline `--body` strings.
- Preferred method: `--body-file` with stdin or a temporary file containing real newlines.
- After posting or editing a comment, verify the rendered/stored body does not contain literal `\\n`; if it does, fix it immediately.

## Remote and Push Policy (Required)
- Treat the user's fork remote as the only writable remote for this workspace.
- Never push to `hk-modding/api`.
- Never push to `upstream` if such a remote is later added and points at `hk-modding/api`.
- Pushing to the fork remote is allowed only if the user explicitly asks for it.
- Do not open PRs unless the user explicitly asks for one.

## Repo Boundary Rule (Required)
- Do not modify any repository other than `/codex/ModdingAPI`.
- You may read other local repositories for reference when the user explicitly points to them, but do not edit them.
- Do not modify `/codex/hollow_knight_analysis` or `/codex/HollowKnight.DebugMod` from this workflow unless the user explicitly changes scope.

## Privilege Escalation and Sudo Policy (Required)
- Do not run `sudo` commands directly.
- If root/system changes are needed, stop and tell the user exactly what command to run.
- After the user confirms the command has been run, continue with non-root steps.

## Execution Logging and State Hygiene (Required)
- Treat substantial tasks as compaction-prone and persist concise local state at meaningful checkpoints.
- Before first code change for an approved implementation step, log the intended approach and acceptance checks on the tracking issue.
- During implementation, log meaningful progress events:
  - milestone reached,
  - important discovery,
  - blocker,
  - scope/plan adjustment,
  - validation result.
- At completion of a work slice, log:
  - what changed,
  - files/areas affected,
  - build/runtime validation status,
  - known limitations,
  - next recommended actions.

## Compaction Handoff Procedure (Required)
- Maintain a local handoff file at `/codex/ModdingAPI/.codex/COMPACTION_HANDOFF.md`.
- This handoff file is local-only and must not be committed.
- Refresh it:
  - before ending a substantial work session,
  - after meaningful milestones or plan adjustments,
  - before any context-compaction handoff.
- Include at minimum:
  - active branch,
  - current issue links and milestone comment links,
  - current build/runtime status,
  - known blockers/risks,
  - explicit next actions and useful commands.
