# M-Travel SaaS Brand Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace visible TourKit branding with M-Travel and integrate two management-SaaS auth visuals into the active Razor Pages application.

**Architecture:** `_BrandMark.cshtml` remains the single shared UI source for the mark and wordmark. New deterministic SVG/PNG brand assets live in `wwwroot/img/branding`, while two generated WebP backgrounds and project-owned CSS serve the Razor login/register panels.

**Tech Stack:** ASP.NET Core Razor Pages, Bootstrap/Vuexy layout, SVG, CSS, xUnit, built-in ImageGen.

## Global Constraints

- Visible brand name is exactly `M-Travel`.
- Primary violet is `#7367F0`, deep ink is `#2F2B3D`, and signal cyan is `#00BAD1`.
- Auth imagery communicates CRM, workflow, calendar, analytics, and coordinated operations—not tourism destinations.
- Keep internal `TourKit.*` namespaces, assemblies, APIs, routes, and database identifiers unchanged.
- Preserve forms, validation, authentication behavior, and the existing desktop-only auth visual breakpoint.
- Do not modify unrelated Dashboard work or vendor CSS.

---

### Task 1: Extend Razor brand coverage tests

**Files:**
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Consumes: rendered `/dang-nhap` and `/dang-ky` HTML.
- Produces: regression coverage for `M-Travel` and distinct auth artwork URLs.

- [ ] **Step 1: Run GitNexus upstream impact analysis**

Analyze `RazorPagesSmokeTests`, `Login_page_renders_anonymously`, and `Register_page_uses_its_own_auth_visual`; report risk before editing.

- [ ] **Step 2: Make the tests require the new visible brand**

Add `Assert.Contains("M-Travel", html, StringComparison.Ordinal);` to both auth page tests while keeping the distinct `/img/illustrations/auth-login.webp` and `/img/illustrations/auth-register.webp` assertions.

- [ ] **Step 3: Verify RED**

Run `dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter FullyQualifiedName~RazorPagesSmokeTests --no-restore`.

Expected: both auth tests fail because rendered HTML still contains TourKit.

### Task 2: Replace the auth artwork with management-SaaS visuals

**Files:**
- Replace: `src/TourKit.Api/wwwroot/img/illustrations/auth-login.webp`
- Replace: `src/TourKit.Api/wwwroot/img/illustrations/auth-register.webp`
- Modify: `src/TourKit.Api/Pages/Auth/Login.cshtml`
- Modify: `src/TourKit.Api/Pages/Auth/Register.cshtml`

**Interfaces:**
- Produces: text-free, portrait 1024×1536 WebP visuals loaded by the two Razor pages.

- [ ] **Step 1: Generate login artwork**

Use built-in ImageGen for a dark premium SaaS operations workspace: layered CRM pipeline, task board, calendar, and analytics modules connected by subtle cyan data paths; violet/ink/cyan palette; asymmetrical depth concentrated right/lower-right; left-side negative space; no readable text, letters, logos, people, devices, or tourism scenery.

- [ ] **Step 2: Generate registration artwork**

Use built-in ImageGen for modular interface blocks assembling into one coordinated operating system: workflow nodes, data cards, calendar grid, and charts; violet/ink/cyan palette with a restrained warm activation glow; right/lower-right composition and left-side negative space; no readable text, letters, logos, people, devices, or tourism scenery.

- [ ] **Step 3: Optimize and inspect**

Convert selected outputs to 1024×1536 WebP, inspect for accidental text/watermarks, and replace only the two incorrect landscape assets.

- [ ] **Step 4: Update auth copy**

Replace user-visible TourKit copy with M-Travel and describe operational control, team coordination, and data—not destinations or travel inspiration.

### Task 3: Create and apply the Modular M identity

**Files:**
- Create: `src/TourKit.Api/wwwroot/img/branding/m-travel-mark.svg`
- Create: `src/TourKit.Api/wwwroot/img/branding/m-travel-mark-180.png`
- Modify: `src/TourKit.Api/Pages/Shared/_BrandMark.cshtml`
- Modify: `src/TourKit.Api/Pages/Shared/Layouts/_CommonMasterLayout.cshtml`
- Modify: `src/TourKit.Api/Pages/Shared/Layouts/Sections/_Variables.cshtml`
- Modify: `src/TourKit.Api/Pages/Shared/Layouts/Sections/Footer/_Footer.cshtml`
- Modify: `src/TourKit.Api/Pages/CompanyProfile/Index.cshtml`

**Interfaces:**
- Produces: one shared `M-Travel` mark/wordmark used by sidebar and auth pages, plus matching browser metadata and icons.

- [ ] **Step 1: Run GitNexus upstream impact analysis**

Analyze `_BrandMark`, `_CommonMasterLayout`, and `_Variables` if indexed. Warn before editing on HIGH or CRITICAL risk and verify all direct consumers.

- [ ] **Step 2: Create the SVG mark**

Use a 32×32 violet rounded square. Draw a white M from four connected rounded strokes and place one cyan node at the center joint. Use paths only—no `<text>`—so favicon rendering is deterministic.

- [ ] **Step 3: Create the Apple touch icon**

Rasterize the SVG to `m-travel-mark-180.png` at 180×180 and visually inspect both sizes.

- [ ] **Step 4: Replace visible brand references**

Point `_BrandMark.cshtml` and favicon links to the new assets; change visible text, title, description, variables, footer, and company short-name example to `M-Travel`. Leave code namespaces and internal identifiers unchanged.

### Task 4: Verify rendering and scope

- [ ] **Step 1: Verify GREEN**

Run the focused Razor smoke tests in Release. Expected: 5 tests pass with no auth failures.

- [ ] **Step 2: Build**

Run `dotnet build TourKit.sln -c Release --no-restore`. Expected: success with no errors.

- [ ] **Step 3: Verify brand coverage**

Search user-facing Razor/layout files for stale TourKit display copy, excluding `@model`, `@using`, and fully-qualified technical namespaces.

- [ ] **Step 4: Verify scope**

Run GitNexus change detection if available. If unavailable, record the limitation and inspect `git diff --check`, `git diff --stat`, and exact file diffs while preserving unrelated Dashboard changes.

- [ ] **Step 5: Visual QA**

Verify login/register at desktop width for crop, contrast, and SaaS meaning; verify the panel remains hidden below the Bootstrap `lg` breakpoint; confirm sidebar, favicon, title, footer, and auth wordmark display M-Travel.
