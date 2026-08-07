# Auth Imagery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate and integrate distinct, professional TourKit visuals for the React login and registration screens.

**Architecture:** Two portrait WebP assets live under `web/src/assets/auth/` and are imported directly by the corresponding Vite page component. Both pages use the same decorative image class and CSS overlay, while all authentication and registration behavior remains untouched.

**Tech Stack:** React 18, TypeScript, Vite 5, Vitest, Testing Library, CSS, built-in ImageGen.

## Global Constraints

- Use a hybrid cinematic-travel and restrained-data-overlay visual language.
- Use TourKit violet `#7367f0`, cyan accents, and restrained amber.
- Keep the main visual interest toward the lower-right with negative space for existing copy.
- Do not include people as focal subjects, embedded text, logos, trademarks, or watermarks.
- Preserve the existing mobile behavior that hides `.mk-auth__brand` below 900 px.
- Do not change forms, validation, API calls, routes, or success states.
- Do not modify or discard unrelated working-tree changes.

---

### Task 1: Generate and optimize auth artwork

**Files:**
- Create: `web/src/assets/auth/auth-login.webp`
- Create: `web/src/assets/auth/auth-register.webp`

**Interfaces:**
- Produces: two Vite-importable portrait image modules.

- [ ] **Step 1: Generate the login artwork with built-in ImageGen**

Use this prompt:

```text
Use case: stylized-concept
Asset type: desktop login brand-panel background for TourKit CRM
Primary request: a premium cinematic Vietnamese travel landscape subtly combined with tour-operation data visualization
Scene/backdrop: cool twilight view over limestone islands and calm water, with a refined dotted travel route and two translucent text-free analytics cards
Style/medium: photorealistic editorial travel image with restrained glassmorphism overlays, sophisticated and realistic rather than futuristic
Composition/framing: portrait 2:3; visual interest concentrated in the lower-right and far-right; generous dark negative space across the upper-left and center-left for white interface copy
Lighting/mood: controlled, trustworthy, calm twilight
Color palette: deep violet #7367f0, indigo, restrained cyan highlights
Constraints: no focal people; no readable text; no numbers; no logo; no trademark; no watermark; no device mockup; uncluttered
```

- [ ] **Step 2: Generate the registration artwork with built-in ImageGen**

Use this prompt:

```text
Use case: stylized-concept
Asset type: desktop registration brand-panel background for TourKit CRM
Primary request: a premium cinematic Vietnamese travel landscape suggesting a new business journey, subtly combined with tour-planning data visualization
Scene/backdrop: sunrise over layered mountains and a winding coastal or highland route, with a refined dotted itinerary path and two translucent text-free setup cards
Style/medium: photorealistic editorial travel image with restrained glassmorphism overlays, sophisticated and realistic rather than futuristic
Composition/framing: portrait 2:3; visual interest concentrated in the lower-right and far-right; generous dark negative space across the upper-left and center-left for white interface copy
Lighting/mood: optimistic sunrise, confident and welcoming
Color palette: TourKit violet #7367f0, deep indigo, restrained warm amber and cyan accents
Constraints: no focal people; no readable text; no numbers; no logo; no trademark; no watermark; no device mockup; uncluttered
```

- [ ] **Step 3: Inspect and optimize the selected outputs**

Confirm composition, palette, and absence of text/watermarks. Convert to portrait WebP assets at 1024×1536 while preserving aspect ratio; keep each file reasonably sized for an auth screen.

### Task 2: Add a failing visual-presence test

**Files:**
- Create: `web/src/features/auth/AuthPages.test.tsx`

**Interfaces:**
- Consumes: `LoginPage`, `RegistrationPage`, and their `.mk-auth__visual` markup.
- Produces: regression coverage that each page renders its own decorative asset.

- [ ] **Step 1: Write the failing test**

```tsx
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { MessageProvider } from '../../ui/message';
import { LoginPage } from './LoginPage';
import { RegistrationPage } from '../registration/RegistrationPage';

vi.mock('./AuthContext', () => ({ useAuth: () => ({ login: vi.fn() }) }));
vi.mock('../registration/registrationApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../registration/registrationApi')>();
  return { ...actual, useRegisterTenant: () => ({ mutateAsync: vi.fn() }) };
});

function renderPage(page: React.ReactNode) {
  return render(<MessageProvider><MemoryRouter>{page}</MemoryRouter></MessageProvider>);
}

describe('auth page artwork', () => {
  it('renders distinct decorative images for login and registration', () => {
    const login = renderPage(<LoginPage />);
    expect(login.container.querySelector('.mk-auth__visual')).toHaveAttribute('src', expect.stringContaining('auth-login'));
    login.unmount();

    const registration = renderPage(<RegistrationPage />);
    expect(registration.container.querySelector('.mk-auth__visual')).toHaveAttribute('src', expect.stringContaining('auth-register'));
  });
});
```

- [ ] **Step 2: Run the focused test and confirm failure**

Run: `npm test -- --run src/features/auth/AuthPages.test.tsx` from `web/`.

Expected: FAIL because `.mk-auth__visual` does not exist yet.

### Task 3: Integrate the assets into both pages

**Files:**
- Modify: `web/src/features/auth/LoginPage.tsx`
- Modify: `web/src/features/registration/RegistrationPage.tsx`

**Interfaces:**
- Consumes: `auth-login.webp` and `auth-register.webp` as Vite static imports.
- Produces: one decorative `<img>` in each `.mk-auth__brand` panel.

- [ ] **Step 1: Run GitNexus upstream impact analysis**

Analyze `LoginPage`, `RegistrationPage`, and `AuthBrand`. Stop and warn before editing if any result is HIGH or CRITICAL; update every depth-1 dependent if required.

- [ ] **Step 2: Add the login asset**

Import `authLoginVisual` from `../../assets/auth/auth-login.webp` and add this immediately after `.mk-auth__glow`:

```tsx
<img className="mk-auth__visual" src={authLoginVisual} alt="" aria-hidden="true" />
```

- [ ] **Step 3: Add the registration asset**

Import `authRegisterVisual` from `../../assets/auth/auth-register.webp` and add the same decorative markup inside `AuthBrand`, using `authRegisterVisual`.

- [ ] **Step 4: Run the focused test and confirm pass**

Run: `npm test -- --run src/features/auth/AuthPages.test.tsx` from `web/`.

Expected: PASS with one distinct asset per page.

### Task 4: Add shared presentation and verify the frontend

**Files:**
- Modify: `web/src/styles/marketing.css`

**Interfaces:**
- Consumes: `.mk-auth__visual` from both auth pages.
- Produces: responsive, non-interactive background treatment with protected text contrast.

- [ ] **Step 1: Add the shared image and overlay rules**

Add absolute full-panel image sizing with `object-fit: cover`, image opacity near `0.5`, a dark violet gradient overlay in `.mk-auth__brand::before`, and explicit stacking so `.mk-auth__brandtop`, `.mk-auth__brandmid`, and `.mk-auth__brandfoot` remain above the artwork. Preserve the existing `@media (max-width: 900px)` behavior.

- [ ] **Step 2: Run automated verification**

From `web/`, run:

```powershell
npm test -- --run src/features/auth/AuthPages.test.tsx
npm run lint
npm run build
```

Expected: focused test, lint, TypeScript, and Vite production build all succeed.

- [ ] **Step 3: Run scope verification**

Use GitNexus change detection if available; otherwise record that the MCP operation is unavailable and inspect `git diff --check`, `git diff --stat`, and `git status --short`. Confirm only the two auth components, shared auth CSS, two assets, test, design spec, and plan are in this task's scope; preserve unrelated dashboard changes.

- [ ] **Step 4: Perform visual QA**

Open `/login` and `/register` at desktop width and verify: white-copy contrast, no overlap with the forms, intentional cropping, distinct scenes, and no visible generated text or watermark. Check a viewport below 900 px to confirm the brand artwork is hidden with the panel.
