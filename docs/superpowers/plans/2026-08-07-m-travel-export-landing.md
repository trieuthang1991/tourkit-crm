# M-Travel Export Landing Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reproduce the approved `D:\MiGroup\AI\tourkit-crm\export` landing page exactly inside the public M-Travel Razor Page.

**Architecture:** The export HTML becomes the Razor page, with only Razor metadata, branded asset paths, and real authentication routes adapted. Tailwind utilities are compiled to a self-hosted stylesheet, while the exported supplemental CSS and JavaScript retain their original presentation and interactions.

**Tech Stack:** ASP.NET Core Razor Pages, Tailwind CSS 3 CLI, vanilla CSS, vanilla JavaScript, xUnit, Playwright CLI.

## Global Constraints

- Preserve the export's layout, text, section order, colors, spacing, responsive behavior, and interactions.
- Keep `/` anonymous, map login to `/dang-nhap`, and map trial CTAs to `/dang-ky`.
- Do not load Tailwind Play CDN or its runtime config in production.
- Do not modify authenticated application pages, APIs, or business workflows.
- Preserve `prefers-reduced-motion` behavior and keyboard-accessible navigation.

---

### Task 1: Lock the landing integration contract

**Files:**
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`
- Test: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Consumes: anonymous `GET /`
- Produces: a regression contract for export-specific copy, self-hosted assets, and real authentication routes

- [ ] **Step 1: Update the failing test**

Assert the response contains `Cả công ty lữ hành trong một hệ thống.`, `M-Travel AI`, `/css/landing.css`, `/js/landing.js`, `/dang-nhap`, and `/dang-ky`. Assert it does not contain `cdn.tailwindcss.com`, `tailwind.config.js`, or `three.module`.

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~Landing_page_renders_export_design_anonymously" --no-restore
```

Expected: FAIL because the current landing does not contain the export headline and AI section.

### Task 2: Integrate the approved export markup and assets

**Files:**
- Modify: `src/TourKit.Api/Pages/Index.cshtml`
- Modify: `src/TourKit.Api/wwwroot/css/landing.css`
- Modify: `src/TourKit.Api/wwwroot/js/landing.js`
- Create temporarily, then remove: `landing.tailwind.input.css`, `tailwind.landing.config.cjs`

**Interfaces:**
- Consumes: `D:\MiGroup\AI\tourkit-crm\export\index.html`, `styles.css`, `tailwind.config.js`, and `script.js`
- Produces: the public Razor page and two self-hosted assets referenced as `/css/landing.css` and `/js/landing.js`

- [ ] **Step 1: Convert the export HTML to Razor**

Keep the export body and utility classes unchanged. Add `@page`, `@model`, canonical/favicons, the M-Travel logo asset, `asp-append-version`, a skip link, and route adaptations. Replace only authentication and asset destinations; preserve visual markup.

- [ ] **Step 2: Build the self-hosted Tailwind stylesheet**

Compile Tailwind against both the Razor markup and JavaScript strings, minify the result, then append the export's supplemental CSS. The output is `src/TourKit.Api/wwwroot/css/landing.css`; no browser-side Tailwind script remains.

- [ ] **Step 3: Port the JavaScript unchanged**

Copy the export interaction data and functions for role tabs, pricing, FAQ, mobile navigation, comparison table, and reveal animations to `src/TourKit.Api/wwwroot/js/landing.js`. Add only accessibility state synchronization and safe reduced-motion fallback where required.

- [ ] **Step 4: Run syntax and focused GREEN checks**

Run:

```powershell
node --check src/TourKit.Api/wwwroot/js/landing.js
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~Landing_page_renders_export_design_anonymously" --no-restore
```

Expected: JavaScript syntax succeeds and the focused test passes.

### Task 3: Verify fidelity and regression safety

**Files:**
- Verify: `src/TourKit.Api/Pages/Index.cshtml`
- Verify: `src/TourKit.Api/wwwroot/css/landing.css`
- Verify: `src/TourKit.Api/wwwroot/js/landing.js`

**Interfaces:**
- Consumes: running landing at `http://127.0.0.1:5080/`
- Produces: visual and automated evidence that the export is integrated without application regressions

- [ ] **Step 1: Run Release build and all tests**

Run `dotnet build TourKit.sln -c Release --no-restore`, then `dotnet test TourKit.sln -c Release --no-build --no-restore`. Expected: zero build errors and all test projects pass.

- [ ] **Step 2: Run browser QA**

Check 1440×960, 393×659, and 320×640. Confirm no horizontal overflow, no console errors, all dynamic panels render, FAQ and pricing toggles work, and authentication links resolve correctly.

- [ ] **Step 3: Run final scope checks**

Run `git diff --check`, `git status --short`, the GitNexus change detector when available, and scan the Razor page for forbidden CDN/Three.js references. Do not commit unrelated worktree changes.
