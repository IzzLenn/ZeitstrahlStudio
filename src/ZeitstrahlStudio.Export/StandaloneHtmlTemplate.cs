namespace ZeitstrahlStudio.Export;

/// <summary>Versionskontrollierte, ressourcenautarke HTML-/CSS-/JavaScript-Vorlage.</summary>
internal static class StandaloneHtmlTemplate
{
    public const string DataPlaceholder = "__ZEITSTRAHL_STUDIO_DATA__";

    public const string Content = """
<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <meta name="referrer" content="no-referrer">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; object-src 'none'; base-uri 'none'; form-action 'none'">
  <title>Zeitstrahl Studio – exportierte Momentaufnahme</title>
  <style>
    :root {
      color-scheme: light;
      --ink: #0f172a;
      --muted: #64748b;
      --panel: #ffffff;
      --line: #cbd5e1;
      --accent: #2563eb;
      --surface: #e2e8f0;
      --axis: #334155;
      font-family: "Segoe UI", Arial, sans-serif;
    }
    * { box-sizing: border-box; }
    body { margin: 0; color: var(--ink); background: #f1f5f9; }
    button, input, select { font: inherit; }
    button { cursor: pointer; }
    .snapshot {
      padding: .7rem 1.25rem;
      color: #78350f;
      background: #fef3c7;
      border-bottom: 1px solid #f59e0b;
      font-size: .9rem;
    }
    .hero {
      display: flex;
      gap: 1.5rem;
      align-items: flex-start;
      justify-content: space-between;
      padding: 1.25rem clamp(1rem, 3vw, 2.5rem);
      color: #fff;
      background: #0f172a;
    }
    .hero h1 { margin: 0; font-size: clamp(1.45rem, 3vw, 2.2rem); }
    .hero p { max-width: 72ch; margin: .45rem 0 0; color: #cbd5e1; }
    .exported-at { flex: none; margin-top: .35rem; color: #94a3b8; font-size: .82rem; }
    .controls {
      display: grid;
      grid-template-columns: repeat(6, minmax(10rem, 1fr));
      gap: .75rem;
      padding: 1rem clamp(1rem, 3vw, 2.5rem);
      background: var(--panel);
      border-bottom: 1px solid var(--line);
    }
    .control { display: flex; min-width: 0; flex-direction: column; gap: .3rem; }
    .control label { color: #475569; font-size: .76rem; font-weight: 700; letter-spacing: .03em; text-transform: uppercase; }
    .control input, .control select {
      width: 100%; height: 2.25rem; padding: 0 .6rem;
      color: var(--ink); background: #fff; border: 1px solid #94a3b8; border-radius: .35rem;
    }
    .control.search { grid-column: span 2; }
    .toolbar {
      display: flex; flex-wrap: wrap; gap: .55rem; align-items: center;
      padding: .75rem clamp(1rem, 3vw, 2.5rem); color: #e2e8f0; background: #1e293b;
    }
    .toolbar button {
      min-height: 2.1rem; padding: 0 .8rem; color: #e2e8f0;
      background: #334155; border: 1px solid #64748b; border-radius: .35rem;
    }
    .toolbar button:hover, .toolbar button:focus-visible { background: #475569; }
    .toolbar button.active { color: #fff; background: var(--accent); border-color: #60a5fa; }
    .toolbar .separator { width: 1px; height: 1.75rem; margin: 0 .15rem; background: #64748b; }
    #zoomLabel, #resultStatus { color: #cbd5e1; font-size: .9rem; }
    #resultStatus { margin-left: auto; }
    .viewport {
      position: relative; height: calc(100vh - 20rem); min-height: 26rem;
      overflow: auto; overscroll-behavior: contain; background: var(--surface); cursor: grab;
    }
    .viewport.dragging { cursor: grabbing; user-select: none; }
    .zoom-surface { position: relative; min-width: 100%; min-height: 100%; }
    .timeline {
      position: absolute; top: 0; left: 0; transform-origin: 0 0;
      padding: 4.5rem 2.5rem 3rem; transition: transform .12s ease-out;
    }
    .timeline.horizontal {
      display: flex; align-items: flex-start; gap: 2rem; width: max-content; min-height: 30rem;
      background: linear-gradient(to bottom, transparent 4.9rem, var(--axis) 4.9rem, var(--axis) 5.08rem, transparent 5.08rem);
    }
    .timeline.vertical {
      display: block; width: min(76rem, calc(100vw - 4rem)); min-width: 38rem;
      background: linear-gradient(to right, transparent 3.45rem, var(--axis) 3.45rem, var(--axis) 3.62rem, transparent 3.62rem);
    }
    .event-item { position: relative; }
    .horizontal .event-item { width: 20rem; padding-top: 1.85rem; }
    .horizontal .event-item.alt { margin-top: 5.5rem; }
    .horizontal .event-item::before {
      content: ""; position: absolute; top: .13rem; left: 50%; width: .8rem; height: .8rem;
      margin-left: -.4rem; background: #fff; border: .22rem solid var(--event-color, var(--accent)); border-radius: 50%;
    }
    .horizontal .event-item::after {
      content: ""; position: absolute; top: .85rem; left: 50%; width: .12rem; height: 1rem; background: var(--axis);
    }
    .vertical .event-item { width: calc(100% - 5.25rem); margin: 0 0 1.25rem 5.25rem; }
    .vertical .event-item::before {
      content: ""; position: absolute; top: 1.25rem; left: -2.19rem; width: .8rem; height: .8rem;
      background: #fff; border: .22rem solid var(--event-color, var(--accent)); border-radius: 50%;
    }
    .vertical .event-item::after {
      content: ""; position: absolute; top: 1.65rem; left: -1.35rem; width: 1.35rem; height: .12rem; background: var(--axis);
    }
    .event-card {
      overflow: hidden; background: var(--panel); border: 1px solid #94a3b8;
      border-left: .38rem solid var(--event-color, var(--accent)); border-radius: .55rem;
      box-shadow: 0 .35rem 1rem rgba(15, 23, 42, .12);
    }
    .event-header { padding: .95rem 1rem .8rem; border-bottom: 1px solid #e2e8f0; }
    .event-date { color: #475569; font-size: .82rem; font-weight: 700; }
    .event-title { margin: .3rem 0 0; font-size: 1.08rem; line-height: 1.3; }
    .event-info { margin: .45rem 0 0; color: #334155; line-height: 1.45; }
    .badges { display: flex; flex-wrap: wrap; gap: .35rem; margin-top: .6rem; }
    .badge { padding: .18rem .45rem; color: #334155; background: #e2e8f0; border-radius: 999px; font-size: .72rem; font-weight: 700; }
    .badge.deadline { color: #7c2d12; background: #ffedd5; }
    .thumbnail { display: block; width: calc(100% - 2rem); max-height: 13rem; margin: .85rem 1rem 0; object-fit: contain; background: #f8fafc; border: 1px solid #cbd5e1; }
    .event-details { padding: .75rem 1rem 1rem; }
    .event-details summary { color: #1d4ed8; font-weight: 700; cursor: pointer; }
    .detail-content { margin-top: .8rem; }
    .detail-content p { margin: .45rem 0; line-height: 1.5; white-space: pre-wrap; overflow-wrap: anywhere; }
    .detail-label { color: #475569; font-weight: 700; }
    .tag-list { display: flex; flex-wrap: wrap; gap: .35rem; margin: .65rem 0; padding: 0; list-style: none; }
    .tag-list li { padding: .18rem .45rem; color: #1e3a8a; background: #dbeafe; border-radius: .3rem; font-size: .78rem; }
    .document-list, .link-list { margin: .4rem 0 .7rem; padding-left: 1.2rem; }
    .document-list li, .link-list li { margin: .3rem 0; overflow-wrap: anywhere; }
    .external-link { color: #1d4ed8; }
    .external-note { margin-left: .35rem; color: #9a3412; font-size: .75rem; font-weight: 700; }
    .gap {
      align-self: center; flex: none; padding: .45rem .65rem; color: #475569; background: #f8fafc;
      border: 1px dashed #64748b; border-radius: .35rem; font-size: .78rem; text-align: center;
    }
    .horizontal .gap { width: 8.5rem; margin-top: .55rem; }
    .vertical .gap { width: calc(100% - 5.25rem); margin: 0 0 1.25rem 5.25rem; }
    .empty { min-width: 28rem; padding: 3rem; color: #475569; background: #fff; border: 1px solid #cbd5e1; border-radius: .5rem; text-align: center; }
    .noscript { margin: 2rem; padding: 1rem; color: #991b1b; background: #fee2e2; border: 1px solid #ef4444; }
    @media (max-width: 1050px) {
      .controls { grid-template-columns: repeat(3, minmax(9rem, 1fr)); }
      .control.search { grid-column: span 2; }
    }
    @media (max-width: 650px) {
      .hero { display: block; }
      .exported-at { margin-top: .8rem; }
      .controls { grid-template-columns: 1fr 1fr; }
      .control.search { grid-column: 1 / -1; }
      #resultStatus { width: 100%; margin-left: 0; }
      .viewport { height: calc(100vh - 28rem); }
      .timeline.vertical { width: calc(100vw - 1rem); min-width: 20rem; padding-left: 1rem; }
      .vertical .event-item, .vertical .gap { width: calc(100% - 4rem); margin-left: 4rem; }
    }
    @media print {
      @page { size: A4 portrait; margin: 12mm; }
      body { color: #000; background: #fff; }
      .controls, .toolbar { display: none !important; }
      .snapshot { color: #000; background: #fff; border: 1px solid #777; }
      .hero { padding: 0 0 8mm; color: #000; background: #fff; border-bottom: 1px solid #555; }
      .hero p, .exported-at { color: #333; }
      .viewport { height: auto; min-height: 0; overflow: visible; background: #fff; }
      .zoom-surface { width: auto !important; height: auto !important; min-height: 0; }
      .timeline, .timeline.horizontal, .timeline.vertical {
        position: static; display: block; width: auto; min-width: 0; min-height: 0;
        padding: 8mm 0 0 8mm; transform: none !important; background: none;
      }
      .event-item, .horizontal .event-item, .horizontal .event-item.alt, .vertical .event-item {
        width: auto; margin: 0 0 5mm; padding: 0; break-inside: avoid; page-break-inside: avoid;
      }
      .event-item::before, .event-item::after { display: none; }
      .event-card { box-shadow: none; border-color: #555; }
      .thumbnail { max-height: 45mm; }
      .gap, .horizontal .gap, .vertical .gap { width: auto; margin: 3mm 0; break-inside: avoid; }
      details > summary { display: none; }
      .external-link { color: #000; text-decoration: none; }
    }
  </style>
</head>
<body>
  <div class="snapshot"><strong>Exportierte Momentaufnahme:</strong> Änderungen in dieser HTML-Datei werden nicht in das Zeitstrahl-Studio-Projekt zurückgeschrieben. Die Datei arbeitet vollständig lokal und sendet keine Daten an externe Dienste.</div>
  <header class="hero">
    <div>
      <h1 id="projectTitle">Zeitstrahl</h1>
      <p id="projectSummary"></p>
    </div>
    <div class="exported-at" id="exportedAt"></div>
  </header>
  <section class="controls" aria-label="Suche und Filter">
    <div class="control search">
      <label for="query">Volltextsuche</label>
      <input id="query" type="search" placeholder="Begriff in Ereignissen oder Dokumenttexten" autocomplete="off">
    </div>
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
    <div class="control">
      <label>&nbsp;</label>
      <button id="resetFilters" type="button">Alle Filter zurücksetzen</button>
    </div>
  </section>
  <nav class="toolbar" aria-label="Darstellung">
    <button id="horizontalButton" type="button">Horizontal</button>
    <button id="verticalButton" type="button">Vertikal</button>
    <span class="separator" aria-hidden="true"></span>
    <button id="zoomOut" type="button" aria-label="Herauszoomen">−</button>
    <span id="zoomLabel">100 %</span>
    <button id="zoomIn" type="button" aria-label="Hineinzoomen">+</button>
    <button id="resetView" type="button">Ansicht zurücksetzen</button>
    <button id="expandAll" type="button">Alle Details öffnen</button>
    <button id="collapseAll" type="button">Alle Details schließen</button>
    <span id="resultStatus" role="status" aria-live="polite"></span>
  </nav>
  <main id="viewport" class="viewport" tabindex="0" aria-label="Interaktiver Zeitstrahl – zum Verschieben ziehen">
    <div id="zoomSurface" class="zoom-surface">
      <section id="timeline" class="timeline" aria-label="Ereignisse"></section>
    </div>
  </main>
  <noscript><div class="noscript">Diese exportierte Datei benötigt lokal aktiviertes JavaScript für Suche, Filter und Darstellungswechsel. Es werden keine Netzwerkverbindungen aufgebaut.</div></noscript>
  <script id="timelineData" type="application/json">__ZEITSTRAHL_STUDIO_DATA__</script>
  <script>
  (function () {
    "use strict";
    var project = JSON.parse(document.getElementById("timelineData").textContent);
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
    var horizontalButton = document.getElementById("horizontalButton");
    var verticalButton = document.getElementById("verticalButton");
    var zoomLabel = document.getElementById("zoomLabel");
    var orientation = project.initialOrientation === "vertical" ? "vertical" : "horizontal";
    var zoom = 1;
    var visibleEvents = project.events.slice();
    var printState = null;
    var dragState = null;

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

    function createEventCard(eventData, index) {
      var item = createElement("article", "event-item" + (index % 2 ? " alt" : ""));
      item.style.setProperty("--event-color", eventData.color);
      item.dataset.eventId = eventData.id;
      var card = createElement("div", "event-card");
      var header = createElement("div", "event-header");
      header.appendChild(createElement("div", "event-date", eventData.dateLabel));
      header.appendChild(createElement("h2", "event-title", eventData.title));
      if (eventData.infoText) { header.appendChild(createElement("p", "event-info", eventData.infoText)); }
      var badges = createElement("div", "badges");
      badges.appendChild(createElement("span", "badge", "Priorität: " + eventData.priority));
      badges.appendChild(createElement("span", "badge", "Status: " + eventData.status));
      badges.appendChild(createElement("span", "badge", "Farbe: " + eventData.color));
      if (eventData.deadline) {
        badges.appendChild(createElement("span", "badge deadline", "Frist " + eventData.deadline.dueDateLabel + " · " + eventData.deadline.statusLabel));
      }
      header.appendChild(badges);
      card.appendChild(header);

      if (eventData.thumbnailDataUrl) {
        var image = createElement("img", "thumbnail");
        image.src = eventData.thumbnailDataUrl;
        image.alt = "Dokumentvorschau zu " + eventData.title;
        image.loading = "lazy";
        card.appendChild(image);
      }

      var details = createElement("details", "event-details");
      details.appendChild(createElement("summary", null, "Details anzeigen"));
      var detailContent = createElement("div", "detail-content");
      appendText(detailContent, "Beschreibung", eventData.description);
      appendText(detailContent, "Notizen", eventData.notes);
      appendText(detailContent, "Quelle", eventData.source);
      if (eventData.deadline) {
        var deadlineText = eventData.deadline.dueDateLabel;
        if (eventData.deadline.dueTime) { deadlineText += " " + eventData.deadline.dueTime; }
        if (eventData.deadline.label) { deadlineText += " · " + eventData.deadline.label; }
        deadlineText += " · " + eventData.deadline.statusLabel;
        appendText(detailContent, "Frist", deadlineText);
        appendText(detailContent, "Fristhinweis", eventData.deadline.reminderNote);
      }
      if (eventData.tags.length) {
        var tags = createElement("ul", "tag-list");
        eventData.tags.forEach(function (tag) { tags.appendChild(createElement("li", null, tag)); });
        detailContent.appendChild(tags);
      }
      if (eventData.attachments.length) {
        detailContent.appendChild(createElement("div", "detail-label", "Dokumentverweise"));
        var documents = createElement("ul", "document-list");
        eventData.attachments.forEach(function (attachment) {
          var suffix = " (" + attachment.mediaType + ", " + formatBytes(attachment.fileSize);
          if (attachment.linkedPdfPage) { suffix += ", Seite " + attachment.linkedPdfPage; }
          documents.appendChild(createElement("li", null, attachment.fileName + suffix + ")"));
        });
        detailContent.appendChild(documents);
      }
      if (eventData.webLinks.length) {
        detailContent.appendChild(createElement("div", "detail-label", "Webseitenlinks"));
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
          listItem.appendChild(createElement("span", "external-note", "↗ Externer Link"));
          links.appendChild(listItem);
        });
        detailContent.appendChild(links);
      }
      details.appendChild(detailContent);
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
        timeline.appendChild(createElement("div", "empty", "Keine Ereignisse entsprechen den aktuellen Filtern."));
        updateZoomSurface();
        return;
      }
      var previousEnd = null;
      visibleEvents.forEach(function (eventData, index) {
        if (previousEnd) {
          var gapDays = daysBetween(previousEnd, eventData.startDate);
          if (gapDays > 1825) {
            var years = Math.floor(gapDays / 365.2425);
            timeline.appendChild(createElement("div", "gap", "Zeitlücke: etwa " + years + " Jahre"));
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
      window.requestAnimationFrame(function () {
        zoomSurface.style.width = Math.max(viewport.clientWidth, Math.ceil(timeline.scrollWidth * zoom)) + "px";
        zoomSurface.style.height = Math.max(viewport.clientHeight, Math.ceil(timeline.scrollHeight * zoom)) + "px";
      });
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
      visibleEvents = project.events.filter(matchesFilters);
      var count = visibleEvents.length;
      resultStatus.textContent = count === 1 ? "1 Ereignis sichtbar" : count + " Ereignisse sichtbar";
      renderTimeline();
    }

    function populateFilters() {
      var colors = Array.from(new Set(project.events.map(function (eventData) { return eventData.color; }))).sort();
      colors.forEach(function (color) { colorFilter.appendChild(createElement("option", null, color)).value = color; });
      var tags = Array.from(new Set(project.events.reduce(function (all, eventData) { return all.concat(eventData.tags); }, []))).sort(function (a, b) { return a.localeCompare(b, "de"); });
      tags.forEach(function (tag) { tagFilter.appendChild(createElement("option", null, tag)).value = tag; });
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

    [queryInput, fromDateInput, untilDateInput, colorFilter, tagFilter, deadlineFilter].forEach(function (control) {
      control.addEventListener(control.tagName === "INPUT" ? "input" : "change", applyFilters);
    });
    document.getElementById("resetFilters").addEventListener("click", resetFilters);
    horizontalButton.addEventListener("click", function () { orientation = "horizontal"; viewport.scrollTo(0, 0); renderTimeline(); });
    verticalButton.addEventListener("click", function () { orientation = "vertical"; viewport.scrollTo(0, 0); renderTimeline(); });
    document.getElementById("zoomOut").addEventListener("click", function () { zoom = Math.max(.5, Math.round((zoom - .1) * 10) / 10); updateZoomSurface(); });
    document.getElementById("zoomIn").addEventListener("click", function () { zoom = Math.min(2.5, Math.round((zoom + .1) * 10) / 10); updateZoomSurface(); });
    document.getElementById("resetView").addEventListener("click", function () { zoom = 1; viewport.scrollTo(0, 0); updateZoomSurface(); });
    document.getElementById("expandAll").addEventListener("click", function () { timeline.querySelectorAll("details").forEach(function (details) { details.open = true; }); updateZoomSurface(); });
    document.getElementById("collapseAll").addEventListener("click", function () { timeline.querySelectorAll("details").forEach(function (details) { details.open = false; }); updateZoomSurface(); });

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
    window.addEventListener("resize", updateZoomSurface);

    window.addEventListener("beforeprint", function () {
      printState = { orientation: orientation, zoom: zoom, openDetails: Array.from(timeline.querySelectorAll("details")).map(function (details) { return details.open; }) };
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
      timeline.querySelectorAll("details").forEach(function (details, index) { details.open = Boolean(printState.openDetails[index]); });
      printState = null;
    });

    document.getElementById("projectTitle").textContent = project.name;
    var projectPeriod = project.overallStart || project.overallEnd
      ? "Projektzeitraum: " + (project.overallStart || "…") + " bis " + (project.overallEnd || "…")
      : null;
    var summaryParts = [project.subtitle, project.infoText, project.description, projectPeriod].filter(function (value) { return value; });
    document.getElementById("projectSummary").textContent = summaryParts.join(" · ");
    document.getElementById("exportedAt").textContent = "Exportiert: " + new Date(project.exportedAtUtc).toLocaleString("de-DE");
    document.title = project.name + " – Zeitstrahl (Momentaufnahme)";
    populateFilters();
    applyFilters();
  }());
  </script>
</body>
</html>
""";
}
