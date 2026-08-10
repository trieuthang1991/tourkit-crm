# M-Travel Product Landing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the root dashboard redirect with a public M-Travel product landing page featuring a self-hosted Three.js hero and real product proof.

**Architecture:** Keep the landing page independent from authenticated layouts. Razor renders accessible content immediately; a page-scoped JavaScript module lazy-loads Three.js only when motion and WebGL are available. CSS supplies the complete static fallback and responsive light/dark design.

**Tech Stack:** ASP.NET Core Razor Pages, native CSS, vanilla JavaScript modules, Three.js 0.184.0, Playwright CLI, xUnit.

## Global Constraints

- Keep `/tong-quan`, `/dang-nhap`, and `/dang-ky` unchanged.
- Use M-Travel violet `#7367F0` as the only accent family.
- Do not use travel destinations, planes, globes, or fabricated customer metrics.
- Honor `prefers-reduced-motion` and `prefers-color-scheme`.
- Do not commit unless GitNexus `detect_changes` succeeds.

---

### Task 1: Root Landing Contract

**Files:**
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`
- Modify: `src/TourKit.Api/Pages/Index.cshtml.cs`

**Interfaces:**
- Consumes: root route `/` and existing `AuthTestFactory`.
- Produces: anonymous HTTP 200 landing response with stable asset and CTA contracts.

- [ ] Run GitNexus impact for `IndexModel`, `OnGet`, `RazorPagesSmokeTests`, and the new test method.
- [ ] Add a failing test:

```csharp
[Fact]
public async Task Landing_page_renders_product_story_anonymously()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var html = await response.Content.ReadAsStringAsync();
    Assert.Contains("M-Travel", html, StringComparison.Ordinal);
    Assert.Contains("landing-hero-canvas", html, StringComparison.Ordinal);
    Assert.Contains("/css/landing.css", html, StringComparison.Ordinal);
    Assert.Contains("/js/landing.js", html, StringComparison.Ordinal);
    Assert.Contains("/dang-ky", html, StringComparison.Ordinal);
    Assert.Contains("/dang-nhap", html, StringComparison.Ordinal);
}
```

- [ ] Run the focused test and confirm it fails because `/` still redirects.
- [ ] Remove the redirecting `OnGet` handler, leaving `IndexModel : PageModel` empty.

### Task 2: Self-Hosted Three.js

**Files:**
- Create: `src/TourKit.Api/wwwroot/vendor/three/three.module.min.js`
- Create: `src/TourKit.Api/wwwroot/vendor/three/LICENSE`

**Interfaces:**
- Produces: `/vendor/three/three.module.min.js`, dynamically imported by landing JavaScript.

- [ ] Fetch package `three@0.184.0` through npm into a temporary directory.
- [ ] Vendor only `build/three.module.min.js` and `LICENSE`.
- [ ] Confirm no CDN URL appears in landing source.

### Task 3: Semantic Landing Page and Visual System

**Files:**
- Replace: `src/TourKit.Api/Pages/Index.cshtml`
- Create: `src/TourKit.Api/wwwroot/css/landing.css`

**Interfaces:**
- Produces: `#landing-hero-canvas`, `[data-reveal]`, and `.landing-hero-static` hooks for JavaScript.

- [ ] Implement metadata, sticky navigation, asymmetric hero, capability rail, five-cell grid, product proof, adoption flow, CTA, and footer.
- [ ] Use the shared M-Travel SVG mark and existing Tabler icon font.
- [ ] Implement explicit 393px, 768px, and 1024px layout fallbacks.
- [ ] Provide light/dark tokens and reduced-motion rules.

### Task 4: Three.js Scene Lifecycle

**Files:**
- Create: `src/TourKit.Api/wwwroot/js/landing.js`

**Interfaces:**
- Consumes: `#landing-hero-canvas`, hero intersection state, pointer position, and media preferences.
- Produces: a modular M scene that initializes, pauses, resumes, resizes, and disposes safely.

- [ ] Implement static reveal behavior with `IntersectionObserver`.
- [ ] Skip the Three.js import when reduced motion is enabled.
- [ ] Create the central M group, orbit rings, connected module blocks, lights, and camera.
- [ ] Cap DPR at `1.6`; use `ResizeObserver` for canvas sizing.
- [ ] Pause animation outside the hero or in a hidden tab.
- [ ] Catch initialization failures and add `is-static` to the hero.

### Task 5: Real Product Proof

**Files:**
- Create: `src/TourKit.Api/wwwroot/img/landing/m-travel-dashboard.webp`

**Interfaces:**
- Produces: a real, optimized dashboard screenshot referenced by the product proof section.

- [ ] Run the application on a separate local port.
- [ ] Sign in with the documented local demo account and capture `/tong-quan` at 1440px.
- [ ] Crop and encode the screenshot as WebP without fabricating UI.
- [ ] Set explicit image width/height and descriptive alt text.

### Task 6: Verification and Review

**Files:**
- Verify all files above.

- [ ] Run focused Razor smoke tests and confirm the red-green cycle finishes green.
- [ ] Build `TourKit.sln` in an isolated Release output directory.
- [ ] Run `git diff --check` and GitNexus `detect_changes` if available.
- [ ] Verify desktop and mobile layouts with Playwright.
- [ ] Emulate dark mode and reduced motion; confirm reduced motion makes no request for Three.js.
- [ ] Check the pre-flight design checklist, visible copy, console errors, asset responses, and horizontal overflow.

