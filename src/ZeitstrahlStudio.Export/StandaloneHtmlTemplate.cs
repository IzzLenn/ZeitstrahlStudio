namespace ZeitstrahlStudio.Export;

/// <summary>Versionskontrollierte, ressourcenautarke HTML-/CSS-/JavaScript-Vorlage.</summary>
internal static class StandaloneHtmlTemplate
{
    public const string DataPlaceholder = "__ZEITSTRAHL_STUDIO_DATA__";

    public const string Content = """
<!doctype html>
<html lang="de" data-theme="light">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <meta name="referrer" content="no-referrer">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'none'; font-src 'none'; media-src 'none'; object-src 'none'; frame-src 'none'; worker-src 'none'; manifest-src 'none'; base-uri 'none'; form-action 'none'">
  <title>Zeitstrahl Studio – exportierte Momentaufnahme</title>
  <style>
    :root {
      color-scheme: light;
      --page: #eef2f7;
      --panel: #ffffff;
      --panel-raised: #ffffff;
      --panel-muted: #f7f9fc;
      --workspace: #e9eef5;
      --workspace-grid: rgba(71, 85, 105, .08);
      --ink: #13213a;
      --ink-strong: #0b1426;
      --muted: #5b6b82;
      --subtle: #64748b;
      --line: #d5dde8;
      --line-strong: #8291a3;
      --accent: #2563eb;
      --accent-hover: #1d4ed8;
      --accent-soft: #dbeafe;
      --accent-ink: #1e3a8a;
      --accent-button-ink: #ffffff;
      --axis: #64748b;
      --axis-soft: #cbd5e1;
      --badge: #edf2f7;
      --badge-ink: #334155;
      --deadline: #fff3d6;
      --deadline-ink: #8a4b08;
      --tag: #e8efff;
      --tag-ink: #274690;
      --warning: #fff7e6;
      --warning-line: #e7a52f;
      --warning-ink: #6f3d06;
      --focus: #2563eb;
      --shadow-sm: 0 1px 2px rgba(15, 23, 42, .08), 0 4px 14px rgba(15, 23, 42, .05);
      --shadow-lg: 0 20px 50px rgba(15, 23, 42, .16);
      --radius-sm: .55rem;
      --radius-md: .85rem;
      --radius-lg: 1.1rem;
      font-family: "Segoe UI Variable", "Segoe UI", Arial, sans-serif;
    }
    :root[data-theme="dark"] {
      color-scheme: dark;
      --page: #0a1220;
      --panel: #111c2e;
      --panel-raised: #17243a;
      --panel-muted: #0f1a2b;
      --workspace: #091323;
      --workspace-grid: rgba(148, 163, 184, .07);
      --ink: #dce6f3;
      --ink-strong: #f5f8fc;
      --muted: #a9b8ca;
      --subtle: #9aacc0;
      --line: #293a52;
      --line-strong: #5f7590;
      --accent: #4f8df7;
      --accent-hover: #73a6ff;
      --accent-soft: #173563;
      --accent-ink: #cfe1ff;
      --accent-button-ink: #0b1426;
      --axis: #758ba5;
      --axis-soft: #30445e;
      --badge: #26364b;
      --badge-ink: #d5dfeb;
      --deadline: #493513;
      --deadline-ink: #ffd88b;
      --tag: #1d3b70;
      --tag-ink: #d6e5ff;
      --warning: #33250d;
      --warning-line: #bc8121;
      --warning-ink: #ffe0a0;
      --focus: #73a6ff;
      --shadow-sm: 0 1px 2px rgba(0, 0, 0, .28), 0 8px 18px rgba(0, 0, 0, .18);
      --shadow-lg: 0 24px 60px rgba(0, 0, 0, .42);
    }
    * { box-sizing: border-box; }
    html, body { width: 100%; height: 100%; }
    body { margin: 0; color: var(--ink); background: var(--page); }
    button, input, select, summary { font: inherit; }
    button, summary { -webkit-tap-highlight-color: transparent; }
    button { cursor: pointer; }
    button:focus-visible, input:focus-visible, select:focus-visible, summary:focus-visible, a:focus-visible, .viewport:focus-visible {
      outline: 3px solid var(--focus); outline-offset: 2px;
    }
    .visually-hidden {
      position: absolute !important; width: 1px !important; height: 1px !important;
      padding: 0 !important; margin: -1px !important; overflow: hidden !important;
      clip: rect(0, 0, 0, 0) !important; white-space: nowrap !important; border: 0 !important;
    }
    .app-shell { display: flex; min-height: 100vh; height: 100vh; flex-direction: column; overflow: hidden; }
    .snapshot {
      display: flex; flex: none; gap: .55rem; align-items: center;
      min-height: 2.35rem; padding: .45rem clamp(1rem, 2.4vw, 2rem);
      color: var(--warning-ink); background: var(--warning); border-bottom: 1px solid var(--warning-line);
      font-size: .82rem; line-height: 1.35;
    }
    .snapshot-label { flex: none; font-weight: 750; letter-spacing: .015em; }
    .project-header {
      display: grid; flex: none; grid-template-columns: minmax(0, 1fr) auto; gap: 1.5rem;
      align-items: center; padding: 1.05rem clamp(1rem, 2.4vw, 2rem);
      color: var(--ink-strong); background: var(--panel); border-bottom: 1px solid var(--line);
    }
    .project-kicker {
      margin: 0 0 .28rem; color: var(--accent); font-size: .72rem; font-weight: 800;
      letter-spacing: .13em; text-transform: uppercase;
    }
    .project-title { margin: 0; max-width: 32ch; font-size: clamp(1.5rem, 2.8vw, 2.25rem); line-height: 1.08; letter-spacing: -.025em; }
    .project-lead { margin: .42rem 0 0; max-width: 78ch; color: var(--muted); font-size: .95rem; line-height: 1.45; }
    .project-context { width: fit-content; max-width: 82ch; margin-top: .5rem; color: var(--muted); font-size: .82rem; }
    .project-context summary { color: var(--accent); font-weight: 700; cursor: pointer; }
    .project-context p { margin: .45rem 0 0; line-height: 1.5; white-space: pre-wrap; }
    .project-metrics { display: grid; grid-template-columns: repeat(3, minmax(8.5rem, 1fr)); gap: .55rem; }
    .metric {
      min-width: 8.5rem; padding: .72rem .82rem; background: var(--panel-muted);
      border: 1px solid var(--line); border-radius: var(--radius-md);
    }
    .metric-label { display: block; color: var(--subtle); font-size: .68rem; font-weight: 800; letter-spacing: .075em; text-transform: uppercase; }
    .metric-value { display: block; margin-top: .22rem; color: var(--ink-strong); font-size: .88rem; font-weight: 750; line-height: 1.25; }
    .control-deck {
      position: relative; z-index: 20; flex: none; background: var(--panel-raised);
      border-bottom: 1px solid var(--line); box-shadow: var(--shadow-sm);
    }
    .toolbar {
      display: flex; gap: .65rem; align-items: center; min-height: 3.25rem;
      padding: .52rem clamp(.75rem, 2vw, 1.5rem); border-bottom: 1px solid var(--line);
    }
    .tool-group { display: inline-flex; flex: none; gap: .3rem; align-items: center; }
    .tool-group + .tool-group { padding-left: .65rem; border-left: 1px solid var(--line); }
    .tool-label { margin-right: .12rem; color: var(--subtle); font-size: .7rem; font-weight: 800; letter-spacing: .075em; text-transform: uppercase; }
    .toolbar button, .filter-summary, .reset-button {
      min-height: 2.15rem; padding: .4rem .72rem; color: var(--ink);
      background: var(--panel-muted); border: 1px solid var(--line-strong); border-radius: var(--radius-sm);
      font-size: .82rem; font-weight: 700; line-height: 1.1;
    }
    .toolbar button:hover, .toolbar button:focus-visible, .filter-summary:hover, .reset-button:hover { background: var(--accent-soft); border-color: var(--accent); }
    .toolbar button.active { color: var(--accent-button-ink); background: var(--accent); border-color: var(--accent); }
    .toolbar button.compact { min-width: 2.15rem; padding-right: .45rem; padding-left: .45rem; font-size: 1rem; }
    #zoomLabel { min-width: 3.7rem; color: var(--muted); font-size: .8rem; font-variant-numeric: tabular-nums; text-align: center; }
    .toolbar-spacer { flex: 1 1 auto; }
    .search-row {
      position: relative; display: grid; grid-template-columns: minmax(16rem, 36rem) auto minmax(9rem, 1fr); gap: .6rem;
      align-items: center; padding: .58rem clamp(.75rem, 2vw, 1.5rem);
    }
    .search-box { position: relative; min-width: 0; }
    .search-box::before {
      content: ""; position: absolute; top: 50%; left: .82rem; width: .65rem; height: .65rem;
      border: 2px solid var(--subtle); border-radius: 50%; transform: translateY(-62%); pointer-events: none;
    }
    .search-box::after {
      content: ""; position: absolute; top: 57%; left: 1.42rem; width: .42rem; height: 2px;
      background: var(--subtle); transform: rotate(45deg); transform-origin: left center; pointer-events: none;
    }
    .search-box input {
      width: 100%; height: 2.35rem; padding: 0 .8rem 0 2.25rem; color: var(--ink-strong);
      background: var(--panel); border: 1px solid var(--line-strong); border-radius: var(--radius-sm);
    }
    .search-box input::placeholder { color: var(--subtle); }
    .filter-disclosure { position: static; }
    .filter-disclosure > summary { list-style: none; cursor: pointer; user-select: none; }
    .filter-disclosure > summary::-webkit-details-marker { display: none; }
    .filter-summary { display: inline-flex; gap: .5rem; align-items: center; white-space: nowrap; }
    .filter-count {
      display: inline-grid; min-width: 1.35rem; height: 1.35rem; padding: 0 .25rem; place-items: center;
      color: var(--accent-button-ink); background: var(--accent); border-radius: 999px; font-size: .68rem;
    }
    .filter-disclosure[open] .filter-summary { color: var(--accent-ink); background: var(--accent-soft); border-color: var(--accent); }
    .filter-popover {
      position: absolute; top: calc(100% + .45rem); right: clamp(.75rem, 2vw, 1.5rem); left: clamp(.75rem, 2vw, 1.5rem); z-index: 40;
      display: grid; grid-template-columns: repeat(5, minmax(9rem, 1fr)); gap: .7rem;
      padding: .9rem; background: var(--panel-raised); border: 1px solid var(--line-strong);
      border-radius: var(--radius-lg); box-shadow: var(--shadow-lg);
    }
    .control { display: flex; min-width: 0; flex-direction: column; gap: .28rem; }
    .control label { color: var(--muted); font-size: .68rem; font-weight: 800; letter-spacing: .055em; text-transform: uppercase; }
    .control input, .control select {
      width: 100%; height: 2.25rem; padding: 0 .62rem; color: var(--ink-strong);
      background: var(--panel); border: 1px solid var(--line-strong); border-radius: var(--radius-sm);
    }
    .filter-actions { display: flex; align-items: end; }
    .reset-button { width: 100%; }
    #resultStatus { justify-self: end; color: var(--muted); font-size: .82rem; font-weight: 650; text-align: right; }
    .viewport {
      position: relative; flex: 1 1 auto; min-height: 0; overflow: auto;
      overscroll-behavior: contain; scrollbar-color: var(--line-strong) var(--workspace);
      background-color: var(--workspace);
      background-image:
        linear-gradient(var(--workspace-grid) 1px, transparent 1px),
        linear-gradient(90deg, var(--workspace-grid) 1px, transparent 1px);
      background-size: 32px 32px; cursor: grab;
    }
    .viewport.dragging { cursor: grabbing; user-select: none; }
    .zoom-surface { position: relative; min-width: 100%; min-height: 100%; }
    .timeline {
      position: absolute; top: 0; left: 0; transform-origin: 0 0;
      transition: transform .14s ease-out;
    }
    .timeline.horizontal {
      display: flex; align-items: stretch; gap: 2.3rem; width: max-content; min-height: 39rem;
      padding: 2.5rem 3.5rem;
      background: linear-gradient(to bottom, transparent calc(50% - 1px), var(--axis) calc(50% - 1px), var(--axis) calc(50% + 1px), transparent calc(50% + 1px));
    }
    .timeline.vertical {
      display: block; width: min(92rem, calc(100vw - 3rem)); min-width: 52rem;
      padding: 2.6rem 2rem 4rem;
      background: linear-gradient(to right, transparent calc(50% - 1px), var(--axis) calc(50% - 1px), var(--axis) calc(50% + 1px), transparent calc(50% + 1px));
    }
    .event-item { position: relative; isolation: isolate; --event-color: var(--accent); }
    .horizontal .event-item {
      display: grid; width: 21.5rem; min-height: 34rem;
      grid-template-rows: minmax(0, 1fr) 3.3rem minmax(0, 1fr);
    }
    .horizontal .event-card { grid-row: 1; align-self: end; }
    .horizontal .event-item.alt .event-card { grid-row: 3; align-self: start; }
    .horizontal .event-item::before {
      content: ""; position: absolute; top: calc(50% - .48rem); left: calc(50% - .48rem); z-index: 4;
      width: .96rem; height: .96rem; background: var(--workspace);
      border: .25rem solid var(--event-color); border-radius: 50%; box-shadow: 0 0 0 .24rem var(--workspace);
    }
    .horizontal .event-item::after {
      content: ""; position: absolute; top: calc(50% - 1.72rem); left: calc(50% - 1px); z-index: -1;
      width: 2px; height: 1.72rem; background: var(--axis);
    }
    .horizontal .event-item.alt::after { top: 50%; }
    .vertical .event-item {
      display: grid; width: 100%; grid-template-columns: minmax(18rem, 1fr) 4.6rem minmax(18rem, 1fr);
      align-items: start; margin-bottom: 1.2rem;
    }
    .vertical .event-card { grid-column: 1; justify-self: stretch; }
    .vertical .event-item.alt .event-card { grid-column: 3; }
    .vertical .event-item::before {
      content: ""; position: absolute; top: 1.35rem; left: calc(50% - .48rem); z-index: 4;
      width: .96rem; height: .96rem; background: var(--workspace);
      border: .25rem solid var(--event-color); border-radius: 50%; box-shadow: 0 0 0 .24rem var(--workspace);
    }
    .vertical .event-item::after {
      content: ""; position: absolute; top: 1.81rem; right: 50%; z-index: -1;
      width: 2.3rem; height: 2px; background: var(--axis);
    }
    .vertical .event-item.alt::after { right: auto; left: 50%; }
    .event-card {
      position: relative; overflow: clip; min-width: 0; color: var(--ink);
      background: var(--panel-raised); border: 2px solid var(--event-color); border-radius: var(--radius-lg);
      box-shadow: 0 0 0 1px var(--line-strong), var(--shadow-sm); transition: box-shadow .15s ease, transform .15s ease;
    }
    .event-card:hover { box-shadow: 0 0 0 1px var(--line-strong), var(--shadow-lg); transform: translateY(-2px); }
    .event-card::before {
      content: ""; display: block; width: 100%; height: .34rem; background: var(--event-color);
    }
    .event-header { padding: .92rem 1rem .82rem; }
    .card-topline { display: flex; gap: .6rem; align-items: center; justify-content: space-between; }
    .event-date { color: var(--ink-strong); font-size: .76rem; font-weight: 850; letter-spacing: .035em; }
    .color-indicator {
      flex: none; width: .78rem; height: .78rem; background: var(--event-color);
      border: 2px solid var(--panel-raised); border-radius: 50%; box-shadow: 0 0 0 1px var(--line-strong);
    }
    .event-title { margin: .38rem 0 0; color: var(--ink-strong); font-size: 1.08rem; line-height: 1.3; letter-spacing: -.01em; }
    .event-info { margin: .48rem 0 0; color: var(--muted); font-size: .88rem; line-height: 1.5; }
    .badges { display: flex; flex-wrap: wrap; gap: .32rem; margin-top: .7rem; }
    .badge {
      padding: .2rem .48rem; color: var(--badge-ink); background: var(--badge);
      border: 1px solid var(--line); border-radius: 999px; font-size: .68rem; font-weight: 750;
    }
    .badge.deadline { color: var(--deadline-ink); background: var(--deadline); border-color: var(--warning-line); }
    .badge.document { color: var(--accent-ink); background: var(--accent-soft); border-color: var(--accent); }
    .thumbnail-wrap { padding: 0 1rem .15rem; }
    .thumbnail {
      display: block; width: 100%; max-height: 12rem; object-fit: contain;
      background: var(--panel-muted); border: 1px solid var(--line); border-radius: var(--radius-sm);
    }
    .event-details { border-top: 1px solid var(--line); }
    .event-details summary {
      position: relative; padding: .72rem 2.4rem .72rem 1rem; color: var(--accent);
      background: var(--panel-muted); font-size: .79rem; font-weight: 800; cursor: pointer; list-style: none;
    }
    .event-details summary::-webkit-details-marker { display: none; }
    .event-details summary::after {
      content: "+"; position: absolute; top: 50%; right: 1rem; color: var(--accent);
      font-size: 1rem; transform: translateY(-52%);
    }
    .event-details[open] summary::after { content: "−"; }
    .event-details summary:hover { color: var(--accent-hover); background: var(--accent-soft); }
    .event-details summary:focus-visible { outline-offset: -3px; }
    .detail-content { padding: .82rem 1rem 1rem; border-top: 1px solid var(--line); }
    .detail-content p { margin: .45rem 0; line-height: 1.55; white-space: pre-wrap; overflow-wrap: anywhere; }
    .detail-label { color: var(--ink-strong); font-weight: 800; }
    .detail-heading { margin: .9rem 0 .35rem; color: var(--ink-strong); font-size: .78rem; letter-spacing: .035em; text-transform: uppercase; }
    .tag-list { display: flex; flex-wrap: wrap; gap: .35rem; margin: .68rem 0; padding: 0; list-style: none; }
    .tag-list li { padding: .22rem .5rem; color: var(--tag-ink); background: var(--tag); border-radius: 999px; font-size: .72rem; font-weight: 700; }
    .document-list, .link-list { margin: .35rem 0 .75rem; padding-left: 1.15rem; }
    .document-list li, .link-list li { margin: .38rem 0; line-height: 1.45; overflow-wrap: anywhere; }
    .external-link { color: var(--accent); font-weight: 700; text-underline-offset: .15em; }
    .external-note { display: inline-block; margin-left: .4rem; color: var(--warning-ink); font-size: .68rem; font-weight: 800; }
    .gap {
      position: relative; z-index: 3; align-self: center; flex: none; padding: .5rem .7rem;
      color: var(--muted); background: var(--panel-raised); border: 1px dashed var(--axis);
      border-radius: var(--radius-sm); box-shadow: var(--shadow-sm); font-size: .72rem; font-weight: 700; text-align: center;
    }
    .horizontal .gap { width: 8.5rem; margin: auto 0; }
    .vertical .gap { width: 16rem; margin: .2rem auto 1.4rem; }
    .empty {
      min-width: min(34rem, calc(100vw - 3rem)); margin: 3rem; padding: 3.5rem 2rem;
      color: var(--muted); background: var(--panel-raised); border: 1px solid var(--line);
      border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); text-align: center;
    }
    .empty strong { display: block; margin-bottom: .4rem; color: var(--ink-strong); font-size: 1.05rem; }
    .noscript { margin: 2rem; padding: 1rem; color: #991b1b; background: #fee2e2; border: 1px solid #ef4444; border-radius: var(--radius-sm); }
    @media (prefers-reduced-motion: reduce) {
      *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; animation-duration: .01ms !important; animation-iteration-count: 1 !important; }
    }
    @media (max-width: 1120px) {
      .project-header { grid-template-columns: 1fr; gap: .85rem; }
      .project-metrics { grid-template-columns: repeat(3, minmax(0, 1fr)); }
      .metric { min-width: 0; }
      .toolbar { overflow-x: auto; }
      .tool-label { display: none; }
      .filter-popover { grid-template-columns: repeat(3, minmax(9rem, 1fr)); }
      .vertical .event-item { grid-template-columns: minmax(18rem, 1fr) 4.6rem minmax(18rem, 1fr); }
    }
    @media (max-width: 760px) {
      .app-shell { height: auto; min-height: 100vh; overflow: visible; }
      .snapshot { align-items: flex-start; }
      .project-header { padding-top: .85rem; padding-bottom: .85rem; }
      .project-title { font-size: 1.55rem; }
      .project-metrics { grid-template-columns: 1fr 1fr; }
      .metric:last-child { grid-column: 1 / -1; }
      .toolbar { flex-wrap: wrap; gap: .45rem; overflow-x: visible; }
      .toolbar-spacer { display: none; }
      .tool-group + .tool-group { padding-left: .4rem; }
      .search-row { grid-template-columns: minmax(0, 1fr) auto; }
      #resultStatus { grid-column: 1 / -1; justify-self: start; text-align: left; }
      .filter-popover { grid-template-columns: 1fr; max-height: 70vh; overflow: auto; }
      .viewport { flex: none; height: 70vh; min-height: 34rem; }
      .timeline.horizontal { min-height: 37rem; padding-right: 1.5rem; padding-left: 1.5rem; }
      .horizontal .event-item { width: min(19.5rem, calc(100vw - 3rem)); }
      .timeline.vertical {
        width: calc(100vw - 1rem); min-width: 20rem; padding: 2rem 1rem 3rem;
        background: linear-gradient(to right, transparent 2.25rem, var(--axis) 2.25rem, var(--axis) calc(2.25rem + 2px), transparent calc(2.25rem + 2px));
      }
      .vertical .event-item { display: block; width: calc(100% - 4.5rem); margin: 0 0 1rem 4.5rem; }
      .vertical .event-card, .vertical .event-item.alt .event-card { display: block; }
      .vertical .event-item::before { left: -2.73rem; }
      .vertical .event-item::after, .vertical .event-item.alt::after { top: 1.81rem; right: auto; left: -2.25rem; width: 2.25rem; }
      .vertical .gap { width: calc(100% - 4.5rem); margin: .2rem 0 1.2rem 4.5rem; }
    }
    @media print {
      @page { size: A4 portrait; margin: 12mm; }
      :root, :root[data-theme="dark"] {
        color-scheme: light;
        --page: #fff; --panel: #fff; --panel-raised: #fff; --panel-muted: #fff; --workspace: #fff; --workspace-grid: transparent;
        --ink: #111; --ink-strong: #111; --muted: #333; --subtle: #444; --line: #aaa; --line-strong: #777;
        --accent: #1d4ed8; --accent-soft: #e5e7eb; --accent-ink: #111; --accent-button-ink: #fff; --axis: #555; --axis-soft: #ddd;
        --badge: #eee; --badge-ink: #111; --deadline: #fff7db; --deadline-ink: #111; --tag: #e8eefc; --tag-ink: #111;
        --warning: #fff; --warning-line: #777; --warning-ink: #111; --shadow-sm: none; --shadow-lg: none;
      }
      * { print-color-adjust: exact; -webkit-print-color-adjust: exact; }
      html, body, .app-shell { width: auto; height: auto; min-height: 0; overflow: visible; }
      body { color: #111; background: #fff; }
      .snapshot { min-height: 0; margin-bottom: 5mm; padding: 2.5mm 3mm; color: #111; background: #fff; border: 1px solid #777; }
      .project-header { display: block; padding: 0 0 6mm; color: #111; background: #fff; border-bottom: 1px solid #555; }
      .project-kicker, .project-lead, .project-context, .metric-label { color: #333; }
      .project-context summary { display: none; }
      .project-metrics { display: flex; gap: 3mm; margin-top: 4mm; }
      .metric { padding: 2mm 3mm; background: #fff; border-color: #999; }
      .control-deck { display: none !important; }
      .viewport { height: auto; min-height: 0; overflow: visible; background: #fff; }
      .zoom-surface { width: auto !important; height: auto !important; min-height: 0; }
      .timeline, .timeline.horizontal, .timeline.vertical {
        position: static; display: block; width: auto; min-width: 0; min-height: 0;
        padding: 6mm 0 0; transform: none !important; background: none;
      }
      .event-item, .horizontal .event-item, .vertical .event-item, .vertical .event-item.alt {
        display: block; width: auto; min-height: 0; margin: 0 0 5mm; padding: 0;
        break-inside: avoid; page-break-inside: avoid;
      }
      .event-item::before, .event-item::after { display: none; }
      .event-card, .event-card:hover { color: #111; background: #fff; box-shadow: none; transform: none; }
      .event-title, .detail-label, .detail-heading { color: #111; }
      .event-info, .detail-content { color: #333; }
      .event-details summary { display: none; }
      .detail-content { border-top: 1px solid #aaa; }
      .thumbnail { max-height: 45mm; }
      .gap, .horizontal .gap, .vertical .gap { width: auto; margin: 3mm 0; box-shadow: none; break-inside: avoid; }
      .external-link { color: #000; text-decoration: none; }
    }
  </style>
</head>
<body>
  <div class="app-shell">
    <aside class="snapshot" aria-label="Hinweis zur Momentaufnahme">
      <span class="snapshot-label">Exportierte Momentaufnahme</span>
      <span>Änderungen in dieser Datei werden nicht in das Zeitstrahl-Studio-Projekt zurückgeschrieben. Die Datei arbeitet vollständig lokal und sendet keine Daten an externe Dienste.</span>
    </aside>
    <header class="project-header">
      <div class="project-copy">
        <p class="project-kicker">Zeitstrahl Studio · Standalone-Export</p>
        <h1 id="projectTitle" class="project-title">Zeitstrahl</h1>
        <p id="projectSummary" class="project-lead"></p>
        <details id="projectContext" class="project-context">
          <summary>Projektbeschreibung anzeigen</summary>
          <p id="projectInfo"></p>
          <p id="projectDescription"></p>
        </details>
      </div>
      <div class="project-metrics" aria-label="Projektkennzahlen">
        <div class="metric"><span class="metric-label">Ereignisse</span><span id="projectEventCount" class="metric-value">0</span></div>
        <div class="metric"><span class="metric-label">Zeitraum</span><span id="projectPeriod" class="metric-value">Nicht festgelegt</span></div>
        <div class="metric"><span class="metric-label">Exportiert</span><span id="exportedAt" class="metric-value"></span></div>
      </div>
    </header>
    <section class="control-deck" aria-label="Zeitstrahlsteuerung">
      <nav class="toolbar" aria-label="Darstellung">
        <div class="tool-group" role="group" aria-label="Ausrichtung">
          <span class="tool-label">Ausrichtung</span>
          <button id="horizontalButton" type="button">Horizontal</button>
          <button id="verticalButton" type="button">Vertikal</button>
        </div>
        <div class="tool-group" role="group" aria-label="Zoom">
          <span class="tool-label">Zoom</span>
          <button id="zoomOut" class="compact" type="button" aria-label="Herauszoomen">−</button>
          <output id="zoomLabel" aria-live="polite">100 %</output>
          <button id="zoomIn" class="compact" type="button" aria-label="Hineinzoomen">+</button>
          <button id="resetView" type="button">Zurücksetzen</button>
        </div>
        <div class="tool-group" role="group" aria-label="Details">
          <span class="tool-label">Details</span>
          <button id="expandAll" type="button">Alle öffnen</button>
          <button id="collapseAll" type="button">Alle schließen</button>
        </div>
        <span class="toolbar-spacer" aria-hidden="true"></span>
        <div class="tool-group" role="group" aria-label="Dateidarstellung">
          <button id="themeButton" type="button" aria-pressed="false">Design: Hell</button>
          <button id="printButton" type="button">Drucken</button>
        </div>
      </nav>
      <div class="search-row">
        <div class="search-box">
          <label class="visually-hidden" for="query">Volltextsuche</label>
          <input id="query" type="search" placeholder="Ereignisse und Dokumenttexte durchsuchen" autocomplete="off">
        </div>
        <details id="filterPanel" class="filter-disclosure">
          <summary class="filter-summary">Filter <span id="filterCount" class="filter-count">0</span></summary>
          <div class="filter-popover" role="group" aria-label="Filteroptionen">
            <div class="control">
              <label for="fromDate">Zeitraum von</label>
              <input id="fromDate" type="date">
            </div>
            <div class="control">
              <label for="untilDate">Zeitraum bis</label>
              <input id="untilDate" type="date">
            </div>
            <div class="control">
              <label for="colorFilter">Farbe</label>
              <select id="colorFilter"><option value="">Alle Farben</option></select>
            </div>
            <div class="control">
              <label for="tagFilter">Schlagwort</label>
              <select id="tagFilter"><option value="">Alle Schlagwörter</option></select>
            </div>
            <div class="control">
              <label for="deadlineFilter">Frist</label>
              <select id="deadlineFilter">
                <option value="">Alle Ereignisse</option>
                <option value="any">Mit Frist</option>
                <option value="none">Ohne Frist</option>
                <option value="open">Frist offen</option>
                <option value="completed">Frist erledigt</option>
                <option value="cancelled">Frist entfallen</option>
              </select>
            </div>
            <div class="filter-actions">
              <button id="resetFilters" class="reset-button" type="button">Filter zurücksetzen</button>
            </div>
          </div>
        </details>
        <span id="resultStatus" role="status" aria-live="polite"></span>
      </div>
    </section>
    <main id="viewport" class="viewport" tabindex="0" aria-label="Interaktiver Zeitstrahl – zum Verschieben ziehen">
      <div id="zoomSurface" class="zoom-surface">
        <section id="timeline" class="timeline" aria-label="Ereignisse"></section>
      </div>
    </main>
    <noscript><div class="noscript">Diese exportierte Datei benötigt lokal aktiviertes JavaScript für Suche, Filter und Darstellungswechsel. Es werden keine Netzwerkverbindungen aufgebaut.</div></noscript>
  </div>
  <script id="timelineData" type="application/json">__ZEITSTRAHL_STUDIO_DATA__</script>
  <script>
  (function () {
    "use strict";
    var project = JSON.parse(document.getElementById("timelineData").textContent);
    var root = document.documentElement;
    var timeline = document.getElementById("timeline");
    var viewport = document.getElementById("viewport");
    var zoomSurface = document.getElementById("zoomSurface");
    var queryInput = document.getElementById("query");
    var fromDateInput = document.getElementById("fromDate");
    var untilDateInput = document.getElementById("untilDate");
    var colorFilter = document.getElementById("colorFilter");
    var tagFilter = document.getElementById("tagFilter");
    var deadlineFilter = document.getElementById("deadlineFilter");
    var resultStatus = document.getElementById("resultStatus");
    var filterCount = document.getElementById("filterCount");
    var filterPanel = document.getElementById("filterPanel");
    var horizontalButton = document.getElementById("horizontalButton");
    var verticalButton = document.getElementById("verticalButton");
    var themeButton = document.getElementById("themeButton");
    var zoomLabel = document.getElementById("zoomLabel");
    var orientation = project.initialOrientation === "vertical" ? "vertical" : "horizontal";
    var zoom = 1;
    var visibleEvents = project.events.slice();
    var printState = null;
    var dragState = null;
    var zoomFrame = null;
    var projectContext = document.getElementById("projectContext");
    function createElement(tagName, className, text) {
      var node = document.createElement(tagName);
      if (className) { node.className = className; }
      if (text !== undefined && text !== null) { node.textContent = text; }
      return node;
    }

    function appendText(parent, label, value) {
      if (value === undefined || value === null || String(value).trim() === "") { return; }
      var paragraph = createElement("p");
      paragraph.appendChild(createElement("span", "detail-label", label + ": "));
      paragraph.appendChild(document.createTextNode(String(value)));
      parent.appendChild(paragraph);
    }

    function formatBytes(bytes) {
      if (bytes < 1024) { return bytes + " B"; }
      if (bytes < 1024 * 1024) { return (bytes / 1024).toFixed(1) + " KiB"; }
      return (bytes / (1024 * 1024)).toFixed(1) + " MiB";
    }

    function formatIsoDate(value) {
      if (!value) { return null; }
      var parsed = new Date(value + "T00:00:00Z");
      if (Number.isNaN(parsed.getTime())) { return value; }
      return parsed.toLocaleDateString("de-DE", { day: "2-digit", month: "2-digit", year: "numeric", timeZone: "UTC" });
    }

    function createBadge(text, extraClass) {
      return createElement("span", "badge" + (extraClass ? " " + extraClass : ""), text);
    }

    function createEventCard(eventData, index) {
      var item = createElement("article", "event-item" + (index % 2 ? " alt" : ""));
      item.style.setProperty("--event-color", eventData.color);
      item.dataset.eventId = eventData.id;
      item.setAttribute("aria-labelledby", "event-title-" + eventData.id);

      var card = createElement("div", "event-card");
      var header = createElement("header", "event-header");
      var topLine = createElement("div", "card-topline");
      var dateElement = createElement("time", "event-date", eventData.dateLabel);
      dateElement.dateTime = eventData.startDate;
      topLine.appendChild(dateElement);
      var colorIndicator = createElement("span", "color-indicator");
      colorIndicator.setAttribute("aria-label", "Ereignisfarbe " + eventData.color);
      colorIndicator.title = "Ereignisfarbe " + eventData.color;
      topLine.appendChild(colorIndicator);
      header.appendChild(topLine);

      var title = createElement("h2", "event-title", eventData.title);
      title.id = "event-title-" + eventData.id;
      header.appendChild(title);
      if (eventData.infoText) { header.appendChild(createElement("p", "event-info", eventData.infoText)); }

      var badges = createElement("div", "badges", null);
      badges.appendChild(createBadge("Priorität: " + eventData.priority));
      badges.appendChild(createBadge("Status: " + eventData.status));
      if (eventData.deadline) {
        badges.appendChild(createBadge("Frist " + eventData.deadline.dueDateLabel + " · " + eventData.deadline.statusLabel, "deadline"));
      }
      if (eventData.attachments.length) {
        var documentLabel = eventData.attachments.length === 1 ? "1 Dokument" : eventData.attachments.length + " Dokumente";
        badges.appendChild(createBadge(documentLabel, "document"));
      }
      header.appendChild(badges);
      card.appendChild(header);

      if (eventData.thumbnailDataUrl) {
        var imageWrap = createElement("div", "thumbnail-wrap");
        var image = createElement("img", "thumbnail");
        image.src = eventData.thumbnailDataUrl;
        image.alt = "Dokumentvorschau zu " + eventData.title;
        image.loading = "lazy";
        imageWrap.appendChild(image);
        card.appendChild(imageWrap);
      }

      var details = createElement("details", "event-details");
      var summary = createElement("summary", null, "Details anzeigen");
      details.appendChild(summary);
      var detailContent = createElement("div", "detail-content");
      appendText(detailContent, "Beschreibung", eventData.description);
      appendText(detailContent, "Notizen", eventData.notes);
      appendText(detailContent, "Quelle", eventData.source);
      appendText(detailContent, "Farbe", eventData.color);
      if (eventData.deadline) {
        var deadlineText = eventData.deadline.dueDateLabel;
        if (eventData.deadline.dueTime) { deadlineText += " " + eventData.deadline.dueTime; }
        if (eventData.deadline.label) { deadlineText += " · " + eventData.deadline.label; }
        deadlineText += " · " + eventData.deadline.statusLabel;
        appendText(detailContent, "Frist", deadlineText);
        appendText(detailContent, "Fristhinweis", eventData.deadline.reminderNote);
      }
      if (eventData.tags.length) {
        detailContent.appendChild(createElement("h3", "detail-heading", "Schlagwörter"));
        var tags = createElement("ul", "tag-list");
        eventData.tags.forEach(function (tag) { tags.appendChild(createElement("li", null, tag)); });
        detailContent.appendChild(tags);
      }
      if (eventData.attachments.length) {
        detailContent.appendChild(createElement("h3", "detail-heading", "Dokumentverweise"));
        var documents = createElement("ul", "document-list");
        eventData.attachments.forEach(function (attachment) {
          var suffix = " (" + attachment.mediaType + ", " + formatBytes(attachment.fileSize);
          if (attachment.linkedPdfPage) { suffix += ", Seite " + attachment.linkedPdfPage; }
          documents.appendChild(createElement("li", null, attachment.fileName + suffix + ")"));
        });
        detailContent.appendChild(documents);
      }
      if (eventData.webLinks.length) {
        detailContent.appendChild(createElement("h3", "detail-heading", "Webseitenlinks"));
        var links = createElement("ul", "link-list");
        eventData.webLinks.forEach(function (webLink) {
          var listItem = createElement("li");
          var anchor = createElement("a", "external-link", webLink.label || webLink.address);
          anchor.href = webLink.address;
          anchor.target = "_blank";
          anchor.rel = "noopener noreferrer";
          anchor.addEventListener("click", function (event) {
            if (!window.confirm("Dieser Link verlässt die lokale HTML-Momentaufnahme und öffnet eine externe Webseite. Fortfahren?")) {
              event.preventDefault();
            }
          });
          listItem.appendChild(anchor);
          listItem.appendChild(createElement("span", "external-note", "Externer Link"));
          links.appendChild(listItem);
        });
        detailContent.appendChild(links);
      }
      if (!detailContent.children.length) {
        detailContent.appendChild(createElement("p", null, "Für dieses Ereignis sind keine weiteren Details hinterlegt."));
      }
      details.appendChild(detailContent);
      details.addEventListener("toggle", function () {
        summary.textContent = details.open ? "Details schließen" : "Details anzeigen";
        updateZoomSurface();
      });
      card.appendChild(details);
      item.appendChild(card);
      return item;
    }

    function daysBetween(first, second) {
      return Math.round((Date.parse(second + "T00:00:00Z") - Date.parse(first + "T00:00:00Z")) / 86400000);
    }

    function renderTimeline() {
      timeline.replaceChildren();
      timeline.className = "timeline " + orientation;
      horizontalButton.classList.toggle("active", orientation === "horizontal");
      verticalButton.classList.toggle("active", orientation === "vertical");
      horizontalButton.setAttribute("aria-pressed", String(orientation === "horizontal"));
      verticalButton.setAttribute("aria-pressed", String(orientation === "vertical"));
      if (!visibleEvents.length) {
        var empty = createElement("div", "empty");
        empty.appendChild(createElement("strong", null, "Keine passenden Ereignisse"));
        empty.appendChild(document.createTextNode("Passen Sie Suche oder Filter an, um wieder Ereignisse anzuzeigen."));
        timeline.appendChild(empty);
        updateZoomSurface();
        return;
      }
      var previousEnd = null;
      visibleEvents.forEach(function (eventData, index) {
        if (previousEnd) {
          var gapDays = daysBetween(previousEnd, eventData.startDate);
          if (gapDays > 1825) {
            var years = Math.floor(gapDays / 365.2425);
            timeline.appendChild(createElement("div", "gap", "Zeitlücke · etwa " + years + " Jahre"));
          }
        }
        timeline.appendChild(createEventCard(eventData, index));
        if (!previousEnd || eventData.endDate > previousEnd) { previousEnd = eventData.endDate; }
      });
      updateZoomSurface();
    }

    function updateZoomSurface() {
      timeline.style.transform = "scale(" + zoom + ")";
      zoomLabel.textContent = Math.round(zoom * 100) + " %";
      if (zoomFrame !== null) { window.cancelAnimationFrame(zoomFrame); }
      zoomFrame = window.requestAnimationFrame(function () {
        zoomSurface.style.width = Math.max(viewport.clientWidth, Math.ceil(timeline.scrollWidth * zoom)) + "px";
        zoomSurface.style.height = Math.max(viewport.clientHeight, Math.ceil(timeline.scrollHeight * zoom)) + "px";
        zoomFrame = null;
      });
    }

    function setZoom(nextZoom) {
      zoom = Math.min(2.5, Math.max(.5, Math.round(nextZoom * 10) / 10));
      updateZoomSurface();
    }

    function activeFilterCount() {
      var values = [queryInput.value.trim(), fromDateInput.value, untilDateInput.value, colorFilter.value, tagFilter.value, deadlineFilter.value];
      return values.filter(function (value) { return Boolean(value); }).length;
    }

    function matchesFilters(eventData) {
      var terms = queryInput.value.toLocaleLowerCase("de-DE").trim().split(/\s+/).filter(Boolean);
      var normalizedText = eventData.searchText.toLocaleLowerCase("de-DE");
      if (!terms.every(function (term) { return normalizedText.indexOf(term) >= 0; })) { return false; }
      var fromDate = fromDateInput.value;
      var untilDate = untilDateInput.value;
      var eventRangeMatches = (!fromDate || eventData.endDate >= fromDate) && (!untilDate || eventData.startDate <= untilDate);
      var deadlineRangeMatches = eventData.deadline && (!fromDate || eventData.deadline.dueDate >= fromDate) && (!untilDate || eventData.deadline.dueDate <= untilDate);
      if ((fromDate || untilDate) && !eventRangeMatches && !deadlineRangeMatches) { return false; }
      if (colorFilter.value && eventData.color !== colorFilter.value) { return false; }
      if (tagFilter.value && eventData.tags.indexOf(tagFilter.value) < 0) { return false; }
      var deadlineValue = deadlineFilter.value;
      if (deadlineValue === "any" && !eventData.deadline) { return false; }
      if (deadlineValue === "none" && eventData.deadline) { return false; }
      if (deadlineValue && deadlineValue !== "any" && deadlineValue !== "none" && (!eventData.deadline || eventData.deadline.status !== deadlineValue)) { return false; }
      return true;
    }

    function applyFilters() {
      var openDetails = currentOpenDetails();
      visibleEvents = project.events.filter(matchesFilters);
      var count = visibleEvents.length;
      var activeCount = activeFilterCount();
      filterCount.textContent = String(activeCount);
      filterPanel.querySelector("summary").setAttribute("aria-label", activeCount === 1 ? "Filter, 1 aktiv" : "Filter, " + activeCount + " aktiv");
      resultStatus.textContent = count === 1
        ? "1 von " + project.events.length + " Ereignissen sichtbar"
        : count + " von " + project.events.length + " Ereignissen sichtbar";
      renderTimeline();
      restoreOpenDetails(openDetails);
    }

    function populateFilters() {
      var colors = Array.from(new Set(project.events.map(function (eventData) { return eventData.color; }))).sort();
      colors.forEach(function (color) {
        var option = createElement("option", null, color);
        option.value = color;
        colorFilter.appendChild(option);
      });
      var tags = Array.from(new Set(project.events.reduce(function (all, eventData) { return all.concat(eventData.tags); }, [])))
        .sort(function (a, b) { return a.localeCompare(b, "de"); });
      tags.forEach(function (tag) {
        var option = createElement("option", null, tag);
        option.value = tag;
        tagFilter.appendChild(option);
      });
    }

    function resetFilters() {
      queryInput.value = "";
      fromDateInput.value = "";
      untilDateInput.value = "";
      colorFilter.value = "";
      tagFilter.value = "";
      deadlineFilter.value = "";
      applyFilters();
      queryInput.focus();
    }

    function preferredTheme() {
      try {
        var storedTheme = window.localStorage.getItem("zeitstrahl-studio-export-theme");
        if (storedTheme === "light" || storedTheme === "dark") { return storedTheme; }
      } catch (error) {
        // Lokale Dateien können je nach Browser einen isolierten Speicher besitzen oder ihn sperren.
      }
      return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function applyTheme(theme, persist) {
      root.dataset.theme = theme;
      var dark = theme === "dark";
      themeButton.textContent = dark ? "Design: Dunkel" : "Design: Hell";
      themeButton.setAttribute("aria-pressed", String(dark));
      themeButton.setAttribute("aria-label", dark ? "Zum hellen Design wechseln" : "Zum dunklen Design wechseln");
      if (persist) {
        try { window.localStorage.setItem("zeitstrahl-studio-export-theme", theme); } catch (error) { }
      }
    }

    function currentOpenDetails() {
      return Array.from(timeline.querySelectorAll("article[data-event-id] details[open]")).map(function (details) {
        return details.closest("article").dataset.eventId;
      });
    }

    function restoreOpenDetails(eventIds) {
      timeline.querySelectorAll("article[data-event-id]").forEach(function (article) {
        if (eventIds.indexOf(article.dataset.eventId) >= 0) {
          var details = article.querySelector("details");
          if (details) { details.open = true; }
        }
      });
    }

    [queryInput, fromDateInput, untilDateInput, colorFilter, tagFilter, deadlineFilter].forEach(function (control) {
      control.addEventListener(control.tagName === "INPUT" ? "input" : "change", applyFilters);
    });
    document.getElementById("resetFilters").addEventListener("click", resetFilters);
    horizontalButton.addEventListener("click", function () { var openDetails = currentOpenDetails(); orientation = "horizontal"; viewport.scrollTo(0, 0); renderTimeline(); restoreOpenDetails(openDetails); });
    verticalButton.addEventListener("click", function () { var openDetails = currentOpenDetails(); orientation = "vertical"; viewport.scrollTo(0, 0); renderTimeline(); restoreOpenDetails(openDetails); });
    document.getElementById("zoomOut").addEventListener("click", function () { setZoom(zoom - .1); });
    document.getElementById("zoomIn").addEventListener("click", function () { setZoom(zoom + .1); });
    document.getElementById("resetView").addEventListener("click", function () { zoom = 1; viewport.scrollTo(0, 0); updateZoomSurface(); });
    document.getElementById("expandAll").addEventListener("click", function () {
      timeline.querySelectorAll("details").forEach(function (details) { details.open = true; });
      updateZoomSurface();
    });
    document.getElementById("collapseAll").addEventListener("click", function () {
      timeline.querySelectorAll("details").forEach(function (details) { details.open = false; });
      updateZoomSurface();
    });
    themeButton.addEventListener("click", function () { applyTheme(root.dataset.theme === "dark" ? "light" : "dark", true); });
    document.getElementById("printButton").addEventListener("click", function () { window.print(); });

    viewport.addEventListener("pointerdown", function (event) {
      if (event.button !== 0 || event.target.closest("button, a, input, select, summary")) { return; }
      dragState = { x: event.clientX, y: event.clientY, left: viewport.scrollLeft, top: viewport.scrollTop, pointerId: event.pointerId };
      viewport.setPointerCapture(event.pointerId);
      viewport.classList.add("dragging");
    });
    viewport.addEventListener("pointermove", function (event) {
      if (!dragState || dragState.pointerId !== event.pointerId) { return; }
      viewport.scrollLeft = dragState.left - (event.clientX - dragState.x);
      viewport.scrollTop = dragState.top - (event.clientY - dragState.y);
    });
    function endDrag(event) {
      if (!dragState || dragState.pointerId !== event.pointerId) { return; }
      dragState = null;
      viewport.classList.remove("dragging");
    }
    viewport.addEventListener("pointerup", endDrag);
    viewport.addEventListener("pointercancel", endDrag);
    viewport.addEventListener("wheel", function (event) {
      if (!event.ctrlKey) { return; }
      event.preventDefault();
      setZoom(zoom + (event.deltaY < 0 ? .1 : -.1));
    }, { passive: false });
    window.addEventListener("resize", updateZoomSurface);

    document.addEventListener("pointerdown", function (event) {
      if (filterPanel.open && !filterPanel.contains(event.target)) { filterPanel.open = false; }
    });
    document.addEventListener("keydown", function (event) {
      var target = event.target;
      var isFormControl = target && /^(INPUT|SELECT|TEXTAREA|BUTTON)$/.test(target.tagName);
      if (event.key === "/" && !isFormControl) {
        event.preventDefault();
        queryInput.focus();
      }
      if (event.key === "Escape" && filterPanel.open) {
        filterPanel.open = false;
        filterPanel.querySelector("summary").focus();
      }
    });

    window.addEventListener("beforeprint", function () {
      if (printState) { return; }
      printState = {
        orientation: orientation,
        zoom: zoom,
        scrollLeft: viewport.scrollLeft,
        scrollTop: viewport.scrollTop,
        filterOpen: filterPanel.open,
        projectContextOpen: projectContext.open,
        openDetails: Array.from(timeline.querySelectorAll("details[open]")).map(function (details) {
          return details.closest("article").dataset.eventId;
        })
      };
      filterPanel.open = false;
      projectContext.open = true;
      orientation = "vertical";
      zoom = 1;
      renderTimeline();
      timeline.querySelectorAll("details").forEach(function (details) { details.open = true; });
    });
    window.addEventListener("afterprint", function () {
      if (!printState) { return; }
      orientation = printState.orientation;
      zoom = printState.zoom;
      renderTimeline();
      restoreOpenDetails(printState.openDetails);
      filterPanel.open = printState.filterOpen;
      projectContext.open = printState.projectContextOpen;
      var restoredScrollLeft = printState.scrollLeft;
      var restoredScrollTop = printState.scrollTop;
      printState = null;
      window.requestAnimationFrame(function () { viewport.scrollTo(restoredScrollLeft, restoredScrollTop); });
    });

    document.getElementById("projectTitle").textContent = project.name;
    document.getElementById("projectSummary").textContent = [project.subtitle, project.infoText].filter(function (value) { return value; }).join(" · ");
    document.getElementById("projectInfo").textContent = project.infoText || "";
    document.getElementById("projectDescription").textContent = project.description || "";
    projectContext.hidden = !project.infoText && !project.description;
    document.getElementById("projectEventCount").textContent = String(project.events.length);
    var periodStart = project.overallStart || (project.events.length ? project.events[0].startDate : null);
    var periodEnd = project.overallEnd || (project.events.length ? project.events[project.events.length - 1].endDate : null);
    var periodLabel = periodStart || periodEnd
      ? (formatIsoDate(periodStart) || "…") + " – " + (formatIsoDate(periodEnd) || "…")
      : "Nicht festgelegt";
    document.getElementById("projectPeriod").textContent = periodLabel;
    document.getElementById("exportedAt").textContent = new Date(project.exportedAtUtc).toLocaleString("de-DE", { dateStyle: "medium", timeStyle: "short" });
    document.title = project.name + " – Zeitstrahl (Momentaufnahme)";
    applyTheme(preferredTheme(), false);
    populateFilters();
    applyFilters();
  }());
  </script>
</body>
</html>
""";
}