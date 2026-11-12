using System.Net;

namespace NaviNavigator;

/// <summary>Self-contained, offline browser portal. No scripts or remote page assets.</summary>
internal static class NaviPortal
{
    internal const string PreviewVersion = "0.0.4";

    internal static string BuildHtml(NaviSkin skin)
    {
        // The approved Navi hero illustration is the primary artwork. The older
        // SVG is retained strictly as a fallback if a ZIP has missing assets.
        string portrait = AssetUri("navi-approved-hero.webp");
        if (string.IsNullOrEmpty(portrait)) portrait = AssetUri("navi-character.svg");
        string emblem = AssetUri("navi-compass.svg");

        string heroArt = string.IsNullOrEmpty(portrait)
            ? "<div class=\"hero-fallback\" aria-label=\"Navi Navigator compass\">✦</div>"
            : "<img class=\"navi-art\" src=\"" + portrait +
              "\" alt=\"Navi the Navigator, an adult teal-and-white fox explorer in a navy-and-ivory navigator jacket, greeting visitors with an outstretched hand\" />";
        string logo = string.IsNullOrEmpty(emblem)
            ? "<span aria-hidden=\"true\">✦</span>"
            : "<img src=\"" + emblem + "\" alt=\"Navi Navigator compass and globe emblem\" />";

        string id = WebUtility.HtmlEncode(skin.Id);
        string name = WebUtility.HtmlEncode(skin.Name);
        string accent = WebUtility.HtmlEncode(skin.Accent);
        string soft = WebUtility.HtmlEncode(skin.AccentLight);

        return $$"""
<!doctype html>
<html lang="en"><head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline';">
<title>Navi Portal</title>
<style>
:root {
 --navy:#0c2c2e; --deep:#0b283e; --teal:#229a9a; --aqua:#6ec6c4;
 --cream:#e6f0f1; --violet:#6a5acd; --accent:{{accent}}; --soft:{{soft}};
 --surface:#e4edf3; --text:#243e52; --pane:#e8f2f7;
 --side1:#e1edf5; --side2:#acc5d6; --border:#8ba8ba;
 --line:#80b9c7; --light:#fff; --card1:#fff; --card2:#d7e8ef;
 --panelradius:9px; --shadow:0 2px 8px #123b5a24;
}
* {box-sizing:border-box}
html {scroll-behavior:smooth}
body {
 margin:0; color:var(--text); background:linear-gradient(135deg,#d7e4ec,#eff5f8 48%,#cddfe9);
 font:14px Tahoma,"Trebuchet MS",Verdana,sans-serif; min-width:320px;
}
a {color:inherit} a:focus-visible {outline:3px solid #86dede;outline-offset:2px}
.masthead {
 position:relative; overflow:hidden; display:flex;align-items:center;gap:16px;
 padding:15px max(20px,calc((100vw - 1580px)/2 + 20px));
 color:#fff; border-bottom:3px solid #74d0d0;
 background:linear-gradient(180deg,#163c55 0%,#0b3850 52%,#087881 100%);
 box-shadow:inset 0 1px #a4e3e455,0 4px 10px #09243730;
}
.masthead::after {
 content:"";position:absolute;right:3%;top:-90px;width:500px;height:240px;pointer-events:none;
 opacity:.20;border:1px solid #b0fafa;border-radius:50%;
 box-shadow:0 0 0 46px #c3ffff14,0 0 0 96px #c3ffff12;
}
.masthead-logo {width:64px;height:64px;flex:0 0 64px;filter:drop-shadow(0 3px 3px #00172566)}
.masthead-logo img {width:100%;height:100%}
.masthead-title {font:bold italic clamp(27px,3.2vw,43px) Georgia,serif;letter-spacing:-.6px;
 text-shadow:1px 3px 2px #03182b;line-height:1}
.masthead-sub {font:bold 10px Tahoma,sans-serif;letter-spacing:3.1px;color:#b2f0ed;margin-top:9px}
.masthead-right {margin-left:auto;text-align:right;position:relative;z-index:1;font-size:11px;
 color:#b9e6ed;letter-spacing:1px}
.shell {display:grid;grid-template-columns:208px minmax(0,1fr);max-width:1580px;margin:0 auto;
 min-height:calc(100vh - 119px);border-left:1px solid #93b0c0;border-right:1px solid #93b0c0}
.sidebar {
 padding:14px 11px 20px;background:linear-gradient(90deg,var(--side1),var(--side2));
 border-right:1px solid #789bab;box-shadow:inset 1px 0 #fff8;
}
.side-brand {display:flex;gap:9px;align-items:center;font-weight:bold;padding:11px 9px 13px;
 color:#173e53;white-space:nowrap}
.side-brand img {width:25px;height:25px}
.side-group {margin-bottom:15px}
.side-title {
 display:flex;justify-content:space-between;align-items:center;
 background:linear-gradient(#f8fcff,#c8dce8);border:1px solid #8baab9;border-radius:3px;
 padding:7px 9px;font-weight:bold;font-size:12px;color:#183d55;
 box-shadow:inset 0 1px #fff,0 1px 2px #6d8b9e3b;
}
.side-title b {color:#177d8a}
.side-link {display:flex;gap:10px;align-items:center;padding:9px 10px;text-decoration:none;
 color:#204c61;border:1px solid transparent;border-radius:3px;font-size:12px;font-weight:600}
.side-link span {font-size:16px;color:#157f8e;min-width:20px;text-align:center}
.side-link:hover {background:linear-gradient(#d4f8f6,#a8dbe3);border-color:#7bafbf;color:#113a50}
.side-note {border-top:1px solid #87adbc;margin-top:18px;padding:16px 9px;color:#527082;font-size:11px;line-height:1.55}
.main {min-width:0;padding:20px clamp(16px,2.2vw,34px) 28px}
.section-caption {display:flex;align-items:center;gap:10px;margin:20px 0 11px;
 font:bold 11px Tahoma,sans-serif;letter-spacing:1.7px;color:#316575;text-transform:uppercase}
.section-caption::after {content:"";height:1px;flex:1;background:linear-gradient(90deg,#78adb9,transparent)}
.hero {
 position:relative;min-height:405px;display:grid;grid-template-columns:minmax(0,58%) minmax(0,42%);
 isolation:isolate;overflow:hidden;border:1px solid #5d9caa;border-radius:var(--panelradius);
 background:linear-gradient(130deg,#05283c,#0f5668 68%,#0a2b43);
 box-shadow:inset 0 1px #cffbf855,0 4px 13px #072c4760;
}
.hero::after {content:"";position:absolute;inset:0;pointer-events:none;
 border:2px solid #a8f7f126;border-radius:inherit}
.hero-photo {height:100%;min-height:405px;overflow:hidden;position:relative;background:#092c3d}
.hero-photo::after {content:"";position:absolute;inset:0;
 background:linear-gradient(90deg,transparent 76%,#0e4353 100%);pointer-events:none}
.navi-art {width:100%;height:100%;display:block;object-fit:cover;object-position:center top}
.hero-fallback {font:130px Georgia,serif;color:#9df4eb;display:grid;place-items:center;height:100%}
.hero-copy {position:relative;padding:34px 28px 25px 14px;display:flex;
 flex-direction:column;justify-content:center;color:#eefdfc}
.hero-copy::before {
 content:"";position:absolute;width:330px;height:270px;right:-100px;top:-135px;opacity:.15;
 border-radius:50%;border:2px solid #b9ffff;box-shadow:0 0 0 26px #9afffa12,0 0 0 59px #9afffa12;
}
.hero-kicker {display:block;color:#98e5e3;letter-spacing:2px;font:bold 10px Tahoma,sans-serif;
 margin-bottom:10px}
.hero h1 {margin:0;font:italic bold clamp(27px,2.4vw,38px) Georgia,serif;line-height:1.12;
 letter-spacing:-.4px;text-shadow:1px 2px 2px #061a2e}
.hero-rule {height:4px;width:145px;border-radius:3px;margin:17px 0;
 background:linear-gradient(90deg,#63c5c7,#c4ffff 65%,transparent)}
.hero p {max-width:440px;margin:0;line-height:1.65;color:#d1eef0;font-size:13px}
.hero-caption {color:#b1f1ed!important;font-style:italic;font-size:15px!important;margin-bottom:12px!important}
.hero-actions {display:flex;flex-wrap:wrap;gap:9px;margin-top:20px}
.hero-actions a {
 display:inline-flex;align-items:center;gap:7px;min-height:35px;text-decoration:none;
 border:1px solid #70c5c8;border-radius:5px;padding:9px 13px;
 color:#fff;background:linear-gradient(#168e9a,#076574);
 box-shadow:inset 0 1px #b1f5f57a,0 2px 3px #00142180;font-weight:700;font-size:12px
}
.hero-actions a.secondary {background:linear-gradient(#31596c,#16394d);border-color:#789dac}
.hero-actions a:hover {background:linear-gradient(#39b4b5,#0d8990)}
.hero-status {color:#a2dbe0;font-size:10px;margin-top:19px;letter-spacing:.25px}
.quicklinks {display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:13px}
.portal-card {
 display:flex;flex-direction:column;text-decoration:none;min-width:0;
 border:1px solid #90b2c2;border-radius:var(--panelradius);
 background:linear-gradient(var(--card1),var(--card2));color:var(--text);
 box-shadow:inset 0 1px var(--light),0 2px 4px #5e7e9130;
 padding:17px 17px 15px;min-height:157px;transition:filter .12s ease,transform .12s ease
}
.portal-card:hover {filter:brightness(1.03);transform:translateY(-2px);border-color:#318e97}
.portal-card-icon {width:42px;height:42px;display:grid;place-items:center;
 color:#fff;background:linear-gradient(#39b7b7,#087184);
 border:1px solid #2d8491;border-radius:11px;font:bold 25px Georgia,serif;
 box-shadow:inset 0 1px #eaffff88,0 2px 2px #0b30412d;margin-bottom:13px}
.portal-card strong {font:bold 15px Tahoma,sans-serif;margin-bottom:6px}
.portal-card small {font-size:12px;line-height:1.4;color:#537083}
.lower {display:grid;grid-template-columns:1.28fr 1fr;gap:13px}
.panel {border:1px solid var(--border);background:linear-gradient(var(--card1),var(--card2));
 border-radius:var(--panelradius);box-shadow:var(--shadow);min-width:0;overflow:hidden}
.panel-head {padding:10px 14px;background:linear-gradient(#d9e8f0,#adcedc);
 border-bottom:1px solid #8cacbc;box-shadow:inset 0 1px #fff;
 color:#143f57;font:bold 13px Tahoma,sans-serif}
.panel-body {padding:14px 16px 17px;line-height:1.55;font-size:12px}
.panel-body p {margin:3px 0 11px}
.panel-body a {color:#0c7785}
.panel-body a:hover {color:#00515a}
.track {display:flex;gap:12px;align-items:center}
.track-mark {font:bold 37px Georgia,serif;color:#168996}
.skin-strips {display:flex;gap:7px;margin-top:12px}
.skin-sample {flex:1;border:1px solid #7d9ca8;border-radius:4px;height:30px;position:relative;
 box-shadow:inset 0 2px #fff9}
.skin-sample.modern {background:linear-gradient(#eaf2f7,#aecada)}
.skin-sample.classic {background:linear-gradient(#f8f8f8,#c6d0d8);border-radius:0}
.skin-sample.fusion {background:linear-gradient(#477e92,#142b43);border-color:#0a2e47}
.skin-sample::after {content:"";position:absolute;left:8px;right:8px;top:10px;
 height:8px;background:#fff7;border-radius:4px}
.foot {
 display:flex;gap:12px;align-items:center;justify-content:space-between;
 margin-top:21px;padding:14px 17px;border:1px solid #88b3bf;border-radius:var(--panelradius);
 color:#f0ffff;background:linear-gradient(100deg,#16465a,#0e777f 60%,#092c45);
 box-shadow:inset 0 1px #afffff53;font-size:12px}
.foot strong {font:italic bold 17px Georgia,serif}.foot span {font-size:10px;color:#b9e1e5}
body[data-skin="classic"] {
 --surface:#e6e8ec; --text:#273c4e; --pane:#edf1f3; --side1:#e8ebef;
 --side2:#c4cbd4; --border:#929fac; --card1:#f7f8fa; --card2:#d3d9e1;
 --panelradius:0px; --shadow:2px 2px 0 #9facb2;
 font-family:Tahoma,Verdana,sans-serif;background:#dae0e6;
}
body[data-skin="classic"] .masthead {background:linear-gradient(#1c526c,#29687d)}
body[data-skin="classic"] .sidebar {background:linear-gradient(90deg,#e6e9ec,#c4cfd9)}
body[data-skin="classic"] .hero,body[data-skin="classic"] .portal-card,
body[data-skin="classic"] .panel {border-radius:0;box-shadow:inset 1px 1px #fff8,2px 2px #8d9fa8}
body[data-skin="classic"] .portal-card-icon,body[data-skin="classic"] .hero-actions a {border-radius:0}
body[data-skin="fusion"] {
 --surface:#1b3a50;--text:#e4f6f6;--side1:#15384d;--side2:#0d253e;
 --border:#286676;--card1:#2b596c;--card2:#173449;--light:#779db044;
 --panelradius:12px;--shadow:0 4px 11px #0008;
 background:linear-gradient(#0a1828,#1d445b);color:#e6f5f7
}
body[data-skin="fusion"] .sidebar {background:linear-gradient(90deg,#173c52,#102b43)}
body[data-skin="fusion"] .side-brand,body[data-skin="fusion"] .side-link {color:#d8f9fa}
body[data-skin="fusion"] .side-link span {color:#72d8d5}
body[data-skin="fusion"] .side-title {background:linear-gradient(#30677b,#173e58);
 border-color:#257787;color:#edffff;box-shadow:inset 0 1px #93efff4b}
body[data-skin="fusion"] .side-note {color:#a4cbd2}
body[data-skin="fusion"] .masthead {background:linear-gradient(#051b2b,#104962 63%,#0b727c)}
body[data-skin="fusion"] .hero {border-color:#4fc4c5;box-shadow:0 0 15px #12a9ad40}
body[data-skin="fusion"] .portal-card {border-color:#40808d;color:#f0ffff}
body[data-skin="fusion"] .portal-card-icon {background:linear-gradient(#37c6c0,#07546b)}
body[data-skin="fusion"] .portal-card small {color:#b6dadf}
body[data-skin="fusion"] .panel-head {background:linear-gradient(#337788,#19465d);
 color:#f0ffff;border-color:#1c8390;box-shadow:inset 0 1px #b7ffff4a}
body[data-skin="fusion"] .panel-body a {color:#8de8e6}
body[data-skin="fusion"] .track-mark {color:#8de8e6}
body[data-skin="fusion"] .skin-sample {border-color:#72b9c2}
@media(max-width:1150px) {
 .hero {grid-template-columns:minmax(0,53%) minmax(0,47%)}
 .hero-copy {padding:25px 18px}
 .quicklinks {grid-template-columns:repeat(2,minmax(0,1fr))}
}
@media(max-width:870px) {
 .shell {grid-template-columns:1fr}
 .sidebar {display:none}
 .main {padding:14px}
 .hero {grid-template-columns:1fr}
 .hero-photo {height:350px;min-height:0}
 .hero-copy {padding:22px 25px 27px}
 .navi-art {object-position:center top}
 .lower {grid-template-columns:1fr}
}
@media(max-width:520px) {
 .masthead {padding:14px;gap:11px}
 .masthead-logo {height:50px;width:50px;flex-basis:50px}
 .masthead-right {display:none}
 .masthead-sub {font-size:8px;letter-spacing:1.6px}
 .quicklinks {grid-template-columns:1fr}
 .hero-photo {height:260px}
 .hero-copy {padding:20px 18px}
 .foot {align-items:flex-start;flex-direction:column}
}
</style>
</head>
<body data-skin="{{id}}">
<header class="masthead">
 <div class="masthead-logo">{{logo}}</div>
 <div><div class="masthead-title">Navi Navigator</div>
  <div class="masthead-sub">THE EXPLORER'S BROWSER</div></div>
 <div class="masthead-right">✦ CHARTING NEW FRONTIERS ✦<br>EST. 2026</div>
</header>
<div class="shell">
 <aside class="sidebar">
  <div class="side-brand">{{logo}} <span>Navi Sidebar</span></div>
  <nav class="side-group" aria-label="Navi services">
   <div class="side-title">Navi Services <b>◆</b></div>
   <a class="side-link" href="https://github.com/BlackShuck89/Navi-Navigator/releases"><span>↓</span> Downloads</a>
   <a class="side-link" href="https://github.com/BlackShuck89/Navi-Navigator"><span>✦</span> Project &amp; News</a>
   <a class="side-link" href="https://github.com/BlackShuck89/Navi-Navigator/issues"><span>✉</span> Feedback</a>
  </nav>
  <nav class="side-group" aria-label="Explore the web">
   <div class="side-title">Explore the Web <b>◆</b></div>
   <a class="side-link" href="https://archive.org/"><span>◉</span> Internet Archive</a>
   <a class="side-link" href="https://en.wikipedia.org/"><span>▤</span> Wikipedia</a>
   <a class="side-link" href="https://www.mozilla.org/"><span>◈</span> Mozilla</a>
  </nav>
  <nav class="side-group" aria-label="Customize Navi">
   <div class="side-title">Customization <b>◆</b></div>
   <a class="side-link" href="https://github.com/BlackShuck89/Navi-Navigator/blob/main/prototype/NaviNavigator/SKINNING.md"><span>✧</span> Create a Skin</a>
   <a class="side-link" href="https://github.com/BlackShuck89/Navi-Navigator/issues"><span>⚙</span> Suggest a Feature</a>
  </nav>
  <div class="side-note">✦ KEEP EXPLORING<br>Home is where the link is.<br>The web is our ocean.</div>
 </aside>
 <main class="main">
  <section class="hero" aria-label="Welcome to the Navi Portal">
   <div class="hero-photo">{{heroArt}}</div>
   <div class="hero-copy">
    <span class="hero-kicker">✦ WELCOME ABOARD</span>
    <h1>Chart your own<br>course through<br>the web.</h1>
    <div class="hero-rule"></div>
    <p class="hero-caption">Meet Navi, your guide to new frontiers.</p>
    <p>Classic menus, expressive navigation, and skins with personality.
    Navi Navigator is bringing back the sense of discovery that made the web exciting.</p>
    <div class="hero-actions">
     <a href="https://archive.org/">Explore the Web&nbsp; ↗</a>
     <a href="https://github.com/BlackShuck89/Navi-Navigator/releases" class="secondary">Get the Latest Build&nbsp; ↓</a>
    </div>
    <div class="hero-status">Navi Navigator {{PreviewVersion}} · {{name}} · Windows technical preview</div>
   </div>
  </section>
  <div class="section-caption">Navi Services <span>✧</span></div>
  <section class="quicklinks" aria-label="Portal services">
   <a class="portal-card" href="https://github.com/BlackShuck89/Navi-Navigator/releases">
     <span class="portal-card-icon">↓</span><strong>Downloads</strong>
     <small>New versions, test releases, and development builds.</small></a>
   <a class="portal-card" href="https://github.com/BlackShuck89/Navi-Navigator">
     <span class="portal-card-icon">◈</span><strong>Navi Project</strong>
     <small>Explore the source code, roadmap, and project news.</small></a>
   <a class="portal-card" href="https://github.com/BlackShuck89/Navi-Navigator/blob/main/prototype/NaviNavigator/SKINNING.md">
     <span class="portal-card-icon">✧</span><strong>Personalize</strong>
     <small>Make your browser your own with installable skins.</small></a>
   <a class="portal-card" href="https://github.com/BlackShuck89/Navi-Navigator/issues">
     <span class="portal-card-icon">✉</span><strong>Community</strong>
     <small>Report a bug, suggest improvements, and join the project.</small></a>
  </section>
  <div class="section-caption">Chart Your Course <span>✧</span></div>
  <section class="lower" aria-label="More from Navi Navigator">
   <div class="panel">
    <div class="panel-head">◈ &nbsp; Discover the Web with Navi</div>
    <div class="panel-body track"><div class="track-mark">✦</div>
      <div><p><strong>Built for the future. Inspired by an adventurous past.</strong></p>
        A familiar desktop interface can still feel personal. Visit the
        <a href="https://archive.org/">Internet Archive</a> to explore the early web,
        or see what is being built for Navi on <a href="https://github.com/BlackShuck89/Navi-Navigator">GitHub</a>.</div>
    </div>
   </div>
   <div class="panel">
    <div class="panel-head">✧ &nbsp; Three Ways to Navigate</div>
    <div class="panel-body">
      <p>Modern, Classic, and Fusion have their own toolbar geometry, tab treatment,
      and portal styling. Switch anytime with <strong>View → Skins</strong>.</p>
      <div class="skin-strips" aria-label="Navi skin color previews">
       <div class="skin-sample modern" title="Navi Modern"></div>
       <div class="skin-sample classic" title="Navi Classic"></div>
       <div class="skin-sample fusion" title="Navi Fusion"></div>
      </div>
    </div>
   </div>
  </section>
  <footer class="foot">
   <strong>Home is where the link is. ✦</strong>
   <span>Navi Navigator {{PreviewVersion}} · WebView2 prototype · Artwork bundled locally · No legacy browser trademarks</span>
  </footer>
 </main>
</div>
</body></html>
""";
    }

    internal static string BuildAboutHtml(NaviSkin skin)
    {
        string art = AssetUri("navi-approved-hero.webp");
        if (string.IsNullOrEmpty(art)) art = AssetUri("navi-character.svg");
        string emblem = AssetUri("navi-compass.svg");
        string image = string.IsNullOrEmpty(art) ? "<span>✦</span>"
            : "<img class=\"art\" src=\"" + art + "\" alt=\"Navi the Navigator\" />";
        string mark = string.IsNullOrEmpty(emblem) ? "✦"
            : "<img class=\"badge\" src=\"" + emblem + "\" alt=\"Navi compass emblem\" />";
        string skinName = WebUtility.HtmlEncode(skin.Name);

        return $$"""
<!doctype html><html lang="en"><head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline';">
<title>About Navi Navigator</title>
<style>
*{box-sizing:border-box}body{margin:0;font:13px Tahoma,sans-serif;background:linear-gradient(#e8f1f5,#bdcfd9);color:#17364b}
header{height:94px;display:flex;align-items:center;gap:14px;padding:16px 25px;
 background:linear-gradient(#173e57,#075e69);color:white;border-bottom:4px solid #7bd3d1}
.badge{width:63px;height:63px}h1{margin:0;font:italic bold 36px Georgia,serif}
.tagline{letter-spacing:3px;font-size:10px;margin:7px 0;color:#baf5ee}
.frame{display:grid;grid-template-columns:41% 59%;margin:17px;border:1px solid #8faeba;
 background:linear-gradient(#fff,#d9e9ed);box-shadow:inset 0 1px white,0 3px 8px #0f384a3b}
.illustration{background:#0b3e50;min-height:306px;overflow:hidden}
.art{width:100%;height:100%;object-fit:cover;object-position:32% center;display:block}
.content{padding:16px 20px 14px;line-height:1.5}
h2{font:italic bold 24px Georgia,serif;color:#174c64;margin:0 0 8px}
.content p{margin:7px 0 12px}strong{color:#086b79}
.spec{display:grid;grid-template-columns:80px auto;gap:6px}
.spec b{color:#41667b}footer{text-align:center;color:#567786;font-size:11px}
</style></head>
<body><header>{{mark}}<div><h1>Navi Navigator</h1>
 <div class="tagline">THE EXPLORER'S BROWSER</div></div></header>
<div class="frame">
 <div class="illustration">{{image}}</div>
 <section class="content">
  <h2>Chart new frontiers.</h2>
  <p><strong>Navi Navigator {{PreviewVersion}}</strong> — Windows technical preview</p>
  <div class="spec"><b>Appearance</b><span>{{skinName}}</span>
   <b>Rendering</b><span>Microsoft WebView2 (Chromium)</span>
   <b>Platform</b><span>Windows x64</span></div>
  <p>The open web should feel like an adventure. Navi brings expressive
     desktop controls and real interface skins to a modern rendering backend.</p>
  <p><strong>Experimental software:</strong> unsigned, without automatic updates.
     Avoid sensitive browsing. The Firefox/Gecko fork is a separate project milestone.</p>
 </section>
</div><footer>Original Navi branding · No affiliation with Netscape, Mozilla, or Microsoft.</footer>
</body></html>
""";
    }

    private static string AssetUri(string filename)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", filename);
        string mime = Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => ""
        };
        if (string.IsNullOrEmpty(mime)) return "";
        try
        {
            var file = new FileInfo(path);
            // WebView2 NavigateToString supports 2 MB of HTML; keep all assets small.
            if (file.Exists && file.Length is > 0 and < 650 * 1024)
                return "data:" + mime + ";base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }
        catch (Exception) { /* Fallback if a packaged resource is unavailable. */ }
        return "";
    }
}
