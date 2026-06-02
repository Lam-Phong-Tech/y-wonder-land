# UNIVERSAL DESIGN SYSTEM TEMPLATE & AI UI GUIDELINES
*(Standardized UI/UX Design System Framework - For Web, Mobile Apps, and Games)*

> [!IMPORTANT]
> **AI AGENT INSTRUCTIONS (Read carefully before creating project-specific designs):**
> 1. This document serves as a **Universal Design System Template**. When tasked with writing a specific design document (`design.md` or `design_system.md`) for any project, you must copy this template and replace all placeholders inside brackets `[...]` with the project's actual design specifications.
> 2. You **MUST NOT remove** Part II (8 AI UI Diseases & Prevention) and Part III (Self-Check Protocol) in the output project document. These guidelines are mandatory to ensure UI quality throughout development.

---

# [Project Design System Name - e.g., The Tangible Playground]

## PART I: DESIGN SYSTEM FRAMEWORK (To be filled per project)

### 1. Overview & Design Philosophy
*   **Project Type:** `[Web / Mobile App / 2D-3D Game]`
*   **Target Platform:** `[Desktop Web / iOS & Android / Cross-platform Game]`
*   **Creative North Star:** `"[Describe the core design philosophy - e.g., Minimalist, Cyberpunk, Playful, Clean & Corporate]"`
*   **UX Principles:**
    *   `[Principle 1 - e.g., Tactile mechanical feedback, zero perceived latency]`
    *   `[Principle 2 - e.g., Action optimization, 3-click rule]`
    *   `[Principle 3 - e.g., Content-first layout, whitespace as visual guide]`

---

### 2. Color System & Contrast Rules

#### 2.1. Color Tokens

You must use these exact color codes. Do not generate unauthorized colors:

| Token | Value (HEX/HSL/RGB) | Usage |
| :--- | :--- | :--- |
| **Primary (Brand)** | `[e.g., #FFD23F]` | Primary Call to Action (CTA) buttons, focal points |
| **Secondary** | `[e.g., #4EA8DE]` | Secondary buttons, supporting cards, categories |
| **Success** | `[e.g., #4CAF50]` | Confirmation, success states, green-lit actions |
| **Warning/Danger** | `[e.g., #E63946]` | Error alerts, dangerous states, destructive actions (Delete) |
| **Background (Layer 0)** | `[e.g., #1E1E24]` | Underlay background of the entire app/game |
| **Surface Deep (Layer 1)**| `[e.g., #2B2D42]` | Primary panels, modals, form containers |
| **Surface Mid (Layer 2)** | `[e.g., #3D405B]` | Sub-cards, list items, input fields |
| **Text Primary** | `[e.g., #F7F7F9]` | Main titles, headings, critical statistics |
| **Text Secondary** | `[e.g., #A8DADC]` | Subtitles, labels, descriptions, timestamps |
| **Border / Outline** | `[e.g., #0F1016]` | Boundaries around interactive elements (if applicable) |

#### 2.2. Contrast Rules & Surface Hierarchy
*   **Contrast Rule:** `[Describe contrast standards. e.g., Text Primary must only be placed on Layer 1 or Layer 2. Maintain a minimum WCAG AA contrast ratio of 4.5:1.]`
*   **Surface Depth Hierarchy:**
    *   **Layer 0 (Background):** `[Color Code]` - Static background.
    *   **Layer 1 (Card/Container):** `[Color Code]` - Grouping container.
    *   **Layer 2 (Element/Interactive):** `[Color Code]` - Interactive elements sitting on top of the container.

---

### 3. Typography & Alignment

*   **Primary Font:** `[Font Name - e.g., Inter, Outfit, Roboto]`
*   **Secondary Font (if any):** `[Font Name - e.g., JetBrains Mono (for numbers/code)]`
*   **Type Scale:**

| Token | Size (rem/px/pt) | Line Height | Weight | Usage |
| :--- | :--- | :--- | :--- | :--- |
| **Heading 1 (H1)** | `[e.g., 2.5rem / 40px]` | `[e.g., 1.2]` | Bold | Main page/screen title |
| **Heading 2 (H2)** | `[e.g., 1.75rem / 28px]`| `[e.g., 1.3]` | Bold | Sub-headers, modal titles |
| **Body Large** | `[e.g., 1.25rem / 20px]`| `[e.g., 1.5]` | SemiBold | Primary button labels, categories |
| **Body Normal** | `[e.g., 1.0rem / 16px]` | `[e.g., 1.5]` | Regular | Body text, descriptions, details |
| **Caption** | `[e.g., 0.75rem / 12px]`| `[e.g., 1.4]` | Regular | Captions, small timestamps, footnotes |

*   **Alignment Rules:**
    *   `[e.g., Heading 1 must be perfectly centered. Form input text is left-aligned. Button text is vertically and horizontally centered.]`

---

### 4. Spacing, Borders & Unit System

The project strictly follows a `[8px or 4px]` grid to ensure spacing consistency.

*   **Spacing Scale:**
    *   `xs` (Extra Small): `[e.g., 4px]` - Micro-spacing (e.g., icon to label spacing).
    *   `sm` (Small): `[e.g., 8px]` - Button padding, inline elements spacing.
    *   `md` (Medium): `[e.g., 16px]` - Standard card padding, gap between content blocks.
    *   `lg` (Large): `[e.g., 24px]` - Panel padding, gap between main columns.
    *   `xl` (Extra Large): `[e.g., 32px]` - Modal margins, spacing between screen sections.
*   **Corner Radius Scale:**
    *   `Large (Modal/Panel)`: `[e.g., 24px]`
    *   `Medium (Card/List item)`: `[e.g., 12px]`
    *   `Small (Button/Input/Tab)`: `[e.g., 8px]`
*   **Border Width Scale (if applicable):**
    *   `Panel Border`: `[e.g., 3px]`
    *   `Button/Input Border`: `[e.g., 2px]`

---

### 5. Elevation, Depth & Interactive States

*   **Elevation & Shadows:**
    *   `Static State (Default)`: `[e.g., Soft drop shadow or rigid retro offset-x: 4px, offset-y: 4px, color #000]`
    *   `Raised State (Hovered/Focused)`: `[e.g., Deeper shadow to simulate elevation towards the viewer]`
*   **Interactive States Specification:**
    *   **Normal:** The default state of the element.
    *   **Hover (Desktop/Web only):** `[e.g., Lighten/darken background by 10% or increase shadow depth]`
    *   **Active / Pressed (Click/Tap):** `[e.g., Translate element down-right by 2px, decrease shadow to 0px to simulate physical force]`
    *   **Focus (Input selected):** `[e.g., Primary color border outline around the box]`
    *   **Disabled:** `[e.g., Opacity 50%, grayed out, pointer-events: none]`

---

### 6. Component Specifications

#### 6.1. Buttons
*   **Primary Button:** Background `[Color]`, Text `[Color]`, Border `[Width/Style]`.
*   **Secondary Button:** Background `[Color]`, Text `[Color]`, Border `[Width/Style]`.
*   **Icon Rule:** `[e.g., Limit icon usage on primary CTA buttons. If used, distance between icon and text is 8px.]`

#### 6.2. Text Inputs
*   **Structure:** Top static label + Input Box with faint Placeholder + Error label underneath.
*   **Height:** `[e.g., 44px on mobile, 40px on web]`.

#### 6.3. Modals & Popups
*   **Overlay:** Underlay barrier color `[Color - e.g., Black at 40% opacity]`.
*   **Dismissal:** Must feature a clear close button (X) in the top-right corner and support clicking outside the modal boundary to close.

---

## PART II: 8 "AI UI DISEASES" & PREVENTION GUIDE

> [!WARNING]
> These are common, critical UI design errors that AI agents frequently make due to copying flashy but impractical designs from online mockups. You must study and avoid these bugs on their respective platforms.

### 1. The "Glassmorphism & Icon Overuse" Addiction
*   **Symptom:** Unnecessary use of `backdrop-filter: blur` and flooding the UI with icons (e.g., adding icons before every header, on every button, and beside every single line of text).
*   **Impact:** Cluttered visual hierarchy, poor text readability, and heavy rendering overhead (especially on mid-range mobile devices).
*   **Prevention Guide:**
    *   **Web:** Restrict blur to sticky navigation headers or modal overlays. Buttons must favor clear Text labels, using icons only for globally understood actions (e.g., gear for Settings, trash can for Delete).
    *   **Mobile App:** Avoid backdrop-blur to conserve battery and GPU cycles. Make touch targets large (min 44px) and rely on clear text instead of tiny, ambiguous icons.
    *   **Game UI:** Do not use glassmorphism unless the style is Sci-Fi/Cyberpunk. Casual, retro, or cartoon games must use solid, high-contrast surfaces.

### 2. Unit Chaos (Mixed Metric Units)
*   **Symptom:** Mixing random measurement units (`px`, `rem`, `em`, `%`, `vh`, `vw`, `pt`) within the same style document without structure.
*   **Impact:** Broken layouts on zoom or when resizing screens.
*   **Prevention Guide:**
    *   **Web:** 
        *   Font-size: Must use `rem` (or `em` for child elements dependent on parent scale) to support browser zoom/accessibility.
        *   Layout & Spacing: Use `rem` or `px` consistently aligned with the Spacing Scale (multiples of 8).
        *   Large containers: Use `%`, `vw`, `vh`, or Flexbox/Grid.
    *   **Mobile App:** Must use standard logical units (e.g., `dp` in Android, `points` in iOS, or unitless dimensions in React Native/Flutter). Never hardcode physical device pixels.
    *   **Game UI (Unity UI Toolkit):** Use `px` for fixed-size elements (padding, borders) and `%` or Flexbox ratios for scaling.

### 3. Color Chaos (Functional Color Neglect)
*   **Symptom:** Using fancy gradients from design portfolios while ignoring functional status colors.
*   **Impact:** Users cannot distinguish safe actions (Save) from destructive ones (Delete) or identify successful actions.
*   **Prevention Guide:**
    *   **All Platforms:** Define and strictly use functional colors (Success = Green, Danger = Red, Warning = Yellow/Orange, Info = Blue). Primary action button colors must be consistent across all views.

### 4. Missing States Failure (Static UI Syndrome)
*   **Symptom:** Designing interactive elements that look great statically, but neglecting Hover, Active, Focus, and Disabled styling.
*   **Impact:** Zero visual response when clicking, making the application feel unresponsive, lagging, or broken.
*   **Prevention Guide:**
    *   **Web:** Elements must define CSS rules for `:hover`, `:active`, `:focus`, and `:disabled`.
    *   **Mobile App:** Program touch feedback (e.g., darken button on tap or scale down slightly) and show loading spinners during active API calls.
    *   **Game UI:** Interactive components must utilize mechanical animation or displacement (e.g., shifting the element down-right and shrinking the hard shadow on click).

### 5. Contrast Failure (Low Contrast Text)
*   **Symptom:** Using light gray text on white backgrounds, or white text on yellow boxes for "aesthetic minimalism".
*   **Impact:** Causes severe eye strain and makes the interface completely unreadable for visually impaired users or under direct sunlight.
*   **Prevention Guide:**
    *   **Web & Mobile App:** Adhere strictly to WCAG 2.1 AA guidelines. Normal text must achieve a minimum contrast ratio of `4.5:1` against the background. Large text (>18pt Bold / >24pt Regular) must hit at least `3:1`.
    *   **Game UI:** If functional art colors restrict WCAG compliance, you must outline text or panels with a dark, high-contrast border (`Border Dark`) of at least 2px.

### 6. Text Input Neglect
*   **Symptom:** Input fields stylized as a single horizontal line or a bare box without labels, placeholders that disappear upon typing, or lack of error text.
*   **Impact:** Users forget what they were typing or cannot figure out which fields contain errors.
*   **Prevention Guide:**
    *   **All Platforms:** Text inputs must feature a static top label (do not replace labels with placeholders), a clear input boundary with Focus highlight, and a dedicated field below for error messaging (e.g., "Invalid email address").

### 7. Layout Cutoff (Text Overrun)
*   **Symptom:** Sizing buttons to perfectly fit default English text (e.g., "Reset"), which breaks when translated to longer languages (e.g., Vietnamese "Làm mới"), cutting off text (e.g., "Làm...").
*   **Impact:** Unreadable UI and broken interactions.
*   **Prevention Guide:**
    *   **All Platforms:** Apply a Safety Margin. The interactive element container width must have a minimum clearance of 20% beyond the default text width. Avoid hardcoding fixed `width` on text-bearing elements; use `min-width` and `padding` instead.

### 8. Visual Hierarchy Mess
*   **Symptom:** Headings (H1) and subheadings (H2) using similar font sizes, or rendering cancel actions in more vibrant colors than confirm actions.
*   **Impact:** Confuses the user's natural reading flow and increases accidental clicks.
*   **Prevention Guide:**
    *   **All Platforms:** Apply clear type scales (e.g., H1 is at least 1.4x the size of H2). Ensure primary actions occupy the highest visual weight (solid primary color), while secondary actions use outline styles or muted tones.

---

## PART III: AI AGENT SELF-CHECK PROTOCOL

*You must run this 15-question checklist before submitting any UI code:*

1.  [ ] **Color System:** Do all colors match the Color Tokens in Section 2.1?
2.  [ ] **Contrast:** Does text meet contrast requirements (WCAG AA 4.5:1 or dark outlines for games)?
3.  [ ] **Typography Harmony:** Are the correct fonts and type scales applied consistently?
4.  [ ] **Grid & Spacing:** Are all spacing dimensions multiples of the grid unit (8px/4px)?
5.  [ ] **Safety Margins:** Have you verified text containers against long translation strings?
6.  [ ] **Interactive States:** Are Hover, Active, Focus, and Disabled styles fully configured?
7.  [ ] **Tactile Feedback:** Do buttons translate/animate physically when pressed?
8.  [ ] **Performance Limits:** Is glassmorphism/blur excluded from performance-critical areas?
9.  [ ] **Icon Minimization:** Have you removed redundant icons and ensured buttons use descriptive text?
10. [ ] **Responsive Design:** Have you verified the layout on diverse aspect ratios (16:9, 4:3, Portrait)?
11. [ ] **Modal Dismissals:** Do overlays include close buttons and support click-outside triggers?
12. [ ] **Structured Inputs:** Do text inputs include static labels and error message fields?
13. [ ] **Unit Consistency:** Have you avoided mixing random units (e.g., using strictly rem/px for web)?
14. [ ] **Action Hierarchy:** Does the primary button carry significantly more visual weight than the secondary?
15. [ ] **Resource Cleanup:** Are UI event listeners unregistered when the interface is closed to prevent memory leaks?
