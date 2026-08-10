# M-Travel Pricing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the landing page's tiered annual/monthly pricing with a free allowance of 20 employees, a 60,000 VND monthly charge for each employee above 20, and a custom option.

**Architecture:** Keep the existing Razor pricing section and vanilla JavaScript renderer. Change only the pricing data, renderer copy, comparison data, and obsolete billing-toggle markup/event handlers; preserve the existing Tailwind classes and animation hooks.

**Tech Stack:** ASP.NET Core Razor Pages, vanilla JavaScript, xUnit, self-hosted Tailwind CSS.

## Global Constraints

- Up to and including 20 employees is free.
- Above 20 employees, charge only the portion above 20 at 60,000 VND per employee per month.
- Show the 30-employee example as 600,000 VND/month.
- Keep three pricing cards and the current reveal/card-hover animations.
- Remove the monthly/annual selector and annual-discount wording.
- Do not commit until the repository-required GitNexus change detection can be run successfully.

---

### Task 1: Lock Pricing Copy with a Razor Page Test

**Files:**
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`
- Test: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Consumes: rendered HTML from `GET /`
- Produces: assertions that define the public pricing contract

- [ ] **Step 1: Run GitNexus impact analysis**

Run impact analysis for `Landing_page_renders_export_design_anonymously` with upstream direction and report direct callers, affected processes, and risk before editing.

- [ ] **Step 2: Write the failing assertions**

Add assertions for `Miễn phí đến 20 nhân viên`, `60.000đ`, `chỉ tính từ nhân viên thứ 21`, `30 nhân viên`, `600.000đ/tháng`, and `Theo yêu cầu`. Add negative assertions for `btn-annual` and `tiết kiệm 2 tháng`.

- [ ] **Step 3: Run the focused test and verify RED**

Run:

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj --filter "FullyQualifiedName~Landing_page_renders_export_design_anonymously"
```

Expected: FAIL because the new pricing copy is not rendered yet.

### Task 2: Implement the Employee-Based Pricing Model

**Files:**
- Modify: `src/TourKit.Api/wwwroot/js/landing.js`
- Modify: `src/TourKit.Api/Pages/Index.cshtml`

**Interfaces:**
- Consumes: `PLANS`, `COMPARE`, `#plans`, and `#compare`
- Produces: three pricing cards and matching comparison rows

- [ ] **Step 1: Run GitNexus impact analysis**

Run upstream impact analysis for `renderPlans` and report risk. Obtain context for `renderCompare` before changing the comparison data.

- [ ] **Step 2: Replace the pricing data**

Define three plans named `Miễn phí`, `Theo quy mô`, and `Theo yêu cầu`. The highlighted middle card must display `60.000đ`, `/người/tháng`, clarify that billing starts with employee 21, and include the 30-person calculation example. Update comparison headings and rows to the same three choices.

- [ ] **Step 3: Simplify the renderer and Razor markup**

Remove the `annual` state, monthly/annual click handlers, and billing selector markup. Keep card classes, hover transforms, reveal hooks, CTA links, and three-column grid unchanged.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run:

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj --filter "FullyQualifiedName~Landing_page_renders_export_design_anonymously"
```

Expected: PASS.

### Task 3: Verify the Landing Page

**Files:**
- Verify: `src/TourKit.Api/Pages/Index.cshtml`
- Verify: `src/TourKit.Api/wwwroot/js/landing.js`
- Verify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Consumes: the completed pricing implementation
- Produces: build, test, browser, and change-scope evidence

- [ ] **Step 1: Run static and automated verification**

Run `node --check src/TourKit.Api/wwwroot/js/landing.js`, `git diff --check`, the full Release build, and all solution tests. Require zero failures.

- [ ] **Step 2: Verify desktop and mobile behavior**

Open `http://127.0.0.1:5080/#bang-gia`; confirm three cards, exact price calculation, responsive layout, working reveal/hover behavior, and zero console errors. Confirm the annual selector is absent.

- [ ] **Step 3: Verify change scope**

Run repository-required GitNexus change detection. If unavailable, do not commit; report the limitation and use `git diff --check`, targeted diffs, and `git status --short` as fallback evidence.
