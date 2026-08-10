# M-Travel Product Landing Design

## Objective

Build a public landing page at `/` that presents M-Travel as a management SaaS operating system. The page must demonstrate technical capability without tourism scenery, invented customer claims, or a fake dashboard.

## Design Direction

Reading this as a B2B SaaS landing page for business owners and operations teams, with a technical-premium visual language built from the existing M-Travel identity.

- `DESIGN_VARIANCE: 8`: asymmetric layouts and deliberate negative space.
- `MOTION_INTENSITY: 7`: interactive Three.js hero plus restrained entry reveals.
- `VISUAL_DENSITY: 4`: enough product detail to build trust without recreating the application UI.
- Existing Public Sans typography and M-Travel violet `#7367F0` remain the brand foundation.
- Use semantic light and dark tokens selected through `prefers-color-scheme`.
- Cards use a consistent 18-24px soft radius; interactive buttons use full pills.

## Information Architecture

1. Sticky navigation with M-Travel mark, product anchors, `Đăng nhập`, and one primary `Dùng thử` CTA.
2. Asymmetric hero with a two-line value proposition and interactive modular 3D system.
3. A compact capability rail explaining the connected operating flow.
4. Five-cell asymmetric capability grid for CRM, operations, finance, tasks, and analytics.
5. Real product proof using a captured screenshot from `/tong-quan`.
6. Three-part adoption flow named by action, not numbered stages.
7. Final CTA and compact footer.

## 3D Experience

Self-host Three.js under `wwwroot/vendor/three/`. The scene represents a central M-shaped operating core connected to floating management modules, not a travel globe or destination.

- Lazy-load the module after the static hero is painted.
- Cap device pixel ratio at `1.6`.
- Use `IntersectionObserver` and `visibilitychange` to stop rendering when the hero is off-screen or the tab is hidden.
- Use pointer parallax only for fine pointer devices.
- Under `prefers-reduced-motion: reduce`, do not import Three.js; retain a polished static CSS composition.
- If WebGL initialization fails, preserve the same static fallback and all content/CTAs.

## Razor Architecture

Replace the current root redirect with an anonymous Razor landing page. `/tong-quan`, `/dang-nhap`, and `/dang-ky` retain their existing behavior. Keep landing code isolated:

- `Pages/Index.cshtml`: semantic page and SEO metadata.
- `Pages/Index.cshtml.cs`: no redirect handler.
- `wwwroot/css/landing.css`: page-scoped design tokens and responsive layout.
- `wwwroot/js/landing.js`: Three.js scene lifecycle and reveal behavior.
- `wwwroot/img/landing/`: real optimized product screenshot.

## Accessibility and Performance

Use visible focus states, keyboard-safe navigation, semantic landmarks, decorative canvas labeling, AA contrast, and no horizontal overflow at 393px. Reserve media dimensions to prevent CLS. The WebGL canvas must not block the initial HTML, CTA, or screenshot. No per-frame DOM state updates and no scroll event listener.

## Verification

- Razor smoke test proves `/` renders anonymously with M-Travel content, login/register links, landing CSS/JS, and the Three.js canvas.
- Build the full solution in Release output isolated from any running API process.
- Browser QA at desktop, mobile, light, dark, and reduced-motion settings.
- Confirm Three.js is requested only in normal-motion mode and rendering stops outside the hero.

