<!--
Guidance for AI coding agents working on the `final-project-website-team-jab` repository.
Keep this file concise and actionable — focus on discoverable patterns and workflows.
-->

# Copilot / Agent Instructions — final-project-website-team-jab

This is a small static website for a Mixed/Virtual Reality final project (Meta Quest 3). The site is intentionally minimal: the app is a static marketing/demo website built from HTML/CSS and static assets.

Key files and folders
- `index.html` — single-page site markup. Contains commented TODO sections for future features (video, features cards). Update title/meta and the content blocks here.
- `styles.css` — complete visual system. Uses CSS variables in `:root` for the color/theme tokens (`--brand`, `--bg`, etc.). Layout rely on utility classes such as `.container`, `.two-col`, `.team`, and `.cards`.
- `img/` — static images (hero, team photos, `favicon.png`). Example assets: `img/concept-blue-contour.png`, `img/johannesliu.jpg`, `img/armaity.jpg`, `img/benjamin.jpeg`.
- `README.md` — currently a TODO placeholder; the repository is published to GitHub Pages (`https://ugavr2025.github.io/final-project-website-team-jab/`).

Big-picture architecture and intent
- This is a static marketing/demo site (no backend code in repo). The site is meant to be served as static files (GitHub Pages or any static host).
- The project metadata in `index.html` (title, description, demo link `https://dma.muscihub.com`) should be kept accurate for SEO and share links.

Developer workflows and commands (explicit)
- Local development: open `index.html` directly in a browser or run a simple file server from the project root. Examples:
  - Python (Windows PowerShell): `python -m http.server 8000` ; then open `http://localhost:8000`.
  - Node (if `serve` installed): `npx serve .` or `npx http-server`.
- Deploy: repository is published via GitHub Pages. Changes pushed to `main` reflect on the existing Pages site (see `README.md`). Confirm Pages settings in the repository settings if unexpected behavior occurs.

Project-specific conventions and patterns
- Visual tokens: use `:root` CSS variables in `styles.css` for color, then reference them in classes (avoid hardcoding new colors unless necessary).
- Layout helpers: prefer the existing `.container`, `.two-col`, `.team`, and `.cards` classes for consistent spacing and responsive behavior.
- Button variants: `.btn`, `.btn.outline`, and `.btn.highlight` are used to distinguish primary/secondary CTAs — reuse these classes for new calls-to-action to maintain visual consistency.
- Commented placeholders: `index.html` contains commented-out sections for features and video — preserve these comments unless replacing with production content.

Integration points & external dependencies
- Fonts: Google Fonts loaded via link in `index.html` (`Inter`). Keep the `preconnect` and `link` if editing typography.
- External demo link: `https://dma.muscihub.com` (in `.btn.highlight`). Treat this as canonical demo destination; verify it when modifying the CTA.
- No JS frameworks, bundlers, or build steps are present. Add tooling only if strictly necessary and document it here.

When making changes
- Small content or style edits: update `index.html` and `styles.css`, preview locally via a static server.
- Adding new images: place under `img/` and reference with relative paths in HTML/CSS. Keep file names lowercase and hyphenated where possible (existing files are mixed; prefer consistency going forward).
- Accessibility: preserve semantic headings (`h1` then `h2`) and provide `alt` attributes for images (existing images have `alt` text — continue this pattern).

Examples from codebase
- Footer year: `index.html` uses small inline JS to set the year: `document.getElementById('year').textContent = new Date().getFullYear();` — keep this if updating the footer.
- Two-column hero layout: `.two-col` grid in `index.html` + `styles.css` controls responsive behavior.

What not to change without confirmation
- Don’t alter the repository’s Pages deployment settings or the `README.md` GitHub Pages URL without asking the maintainers.
- Don’t introduce a build pipeline or package manager unless requested — this repo is intentionally minimal.

If you make edits, include a short PR description that answers:
- Why this change was necessary (content, accessibility, bugfix).
- Preview instructions (how to view locally).
- Any deployment considerations for Pages (if the URL or entry point changed).

Questions or gaps
- README is a placeholder — ask maintainers whether they want a generated README and any canonical deployment instructions.

If anything in this guidance seems unclear, ask a maintainer for confirmation before making structural changes.
