# M-Travel Dub-Style Landing Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the technology-demo landing with a light, product-first Dub-style SaaS landing while preserving public routing and conversion paths.

**Architecture:** Keep the standalone Razor Page and its real dashboard WebP. Recompose the markup, replace the landing stylesheet with the approved token system, and reduce the JavaScript to IntersectionObserver reveals only. Remove the now-unused Three.js vendor payload after the new contract test passes.

**Tech Stack:** ASP.NET Core Razor Pages, native CSS, vanilla ES modules, Tabler Icons, xUnit, Playwright CLI.

## Global Constraints

- Keep `/`, `/dang-nhap`, `/dang-ky`, `#platform`, `#capabilities`, `#product`, and `#adoption` unchanged.
- Use `#ffffff`, `#f5f5f5`, `#e5e5e5`, `#171717`, `#737373`, `#2563eb`, and `#0a0a0a` as the landing palette.
- No Three.js, canvas, 3D, stock photography, fake UI, heavy shadows, automatic dark mode, or em/en dashes.
- Cards use 12px radius, large product surfaces 16px, buttons 8px, and tags 9999px.
- Use the existing real `/img/landing/m-travel-dashboard.webp` screenshot.

---

### Task 1: Lock the Product-First Landing Contract

**Files:**
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`
- Test: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Consumes: anonymous `GET /` through `AuthTestFactory`.
- Produces: a regression contract for the real dashboard, Dub hero, CTA routes, and absence of 3D dependencies.

- [ ] **Step 1: Update the landing smoke test**

Assert that the response contains `landing-dashboard`, `/img/landing/m-travel-dashboard.webp`, `/dang-ky`, and `/dang-nhap`; assert that it does not contain `landing-hero-canvas`, `three.module`, or `/vendor/three/`.

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Landing_page_renders_product_story_anonymously`

Expected: FAIL because the current page still renders `landing-hero-canvas` and imports Three.js.

### Task 2: Recompose the Razor Page

**Files:**
- Modify: `src/TourKit.Api/Pages/Index.cshtml`

**Interfaces:**
- Consumes: existing M-Travel branding assets and dashboard WebP.
- Produces: semantic sections and stable anchors consumed by `landing.css` and `landing.js`.

- [ ] **Step 1: Replace the split 3D hero**

Create a centered hero with eyebrow, two-line headline, concise body, `Dùng thử` and `Xem sản phẩm` actions, three feature pills, and a `landing-dashboard` figure using the real screenshot.

- [ ] **Step 2: Replace decorative sections**

Keep the same section IDs while converting capabilities to an asymmetric five-cell border grid, product proof to a z-pattern screenshot/detail composition, and adoption to one bordered strip.

- [ ] **Step 3: Remove the Three.js module dependency**

Keep `/js/landing.js` as a deferred non-module script for reveal behavior only.

### Task 3: Replace the Visual System

**Files:**
- Modify: `src/TourKit.Api/wwwroot/css/landing.css`

**Interfaces:**
- Consumes: classes emitted by `Index.cshtml`.
- Produces: the supplied Dub token system at desktop, tablet, and mobile widths.

- [ ] **Step 1: Define the light token system**

Implement the exact palette, 1200px shell, 68px nav, 8/12/16/full radius vocabulary, Inter-style body, and 48px weight-500 display scale.

- [ ] **Step 2: Build border-first layouts**

Use hairlines for the nav, dashboard frame, flow rail, capability grid, adoption strip, and footer. Use only the approved product-frame ring shadow.

- [ ] **Step 3: Implement responsive and reduced-motion states**

Declare explicit fallbacks below 1024px, 768px, and 420px; ensure no overflow and reveal content immediately under reduced motion.

### Task 4: Simplify Behavior and Remove 3D Payload

**Files:**
- Modify: `src/TourKit.Api/wwwroot/js/landing.js`
- Delete: `src/TourKit.Api/wwwroot/vendor/three/three.module.min.js`
- Delete: `src/TourKit.Api/wwwroot/vendor/three/three.core.min.js`
- Delete: `src/TourKit.Api/wwwroot/vendor/three/LICENSE`

**Interfaces:**
- Consumes: `[data-reveal]` elements.
- Produces: one-time, reduced-motion-aware reveal behavior with observer cleanup.

- [ ] **Step 1: Replace the script**

Retain only a guarded IntersectionObserver that adds `is-visible`, unobserves revealed elements, and renders everything immediately for reduced motion or unsupported browsers.

- [ ] **Step 2: Remove local Three.js**

Delete only the verified `src/TourKit.Api/wwwroot/vendor/three` directory created for the retired landing scene.

- [ ] **Step 3: Run the focused test and verify GREEN**

Expected: the updated landing smoke test passes.

### Task 5: Verify Product Quality

**Files:**
- Verify: all modified landing and test files.

**Interfaces:**
- Consumes: the running development site.
- Produces: build, test, visual, accessibility, and scope evidence.

- [ ] **Step 1: Run automated verification**

Run `dotnet build TourKit.sln -c Release --no-restore`, `dotnet test TourKit.sln -c Release --no-build --no-restore`, `node --check src/TourKit.Api/wwwroot/js/landing.js`, and `git diff --check`.

- [ ] **Step 2: Run browser verification**

At 1440px, 393px, and 320px verify: no overflow, hero CTA visible, real dashboard loaded, no canvas/Three.js request, zero console errors, and reduced motion leaves all content visible.

- [ ] **Step 3: Run repository scope verification**

Run GitNexus change detection when available. If the installed CLI still lacks the command, record that limitation and use `git status`, targeted `git diff`, and `git diff --check` without committing.

