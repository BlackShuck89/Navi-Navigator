using System.Net;

namespace NaviNavigator;

internal static class NaviPortal
{
    internal static string BuildHtml(NaviSkin skin)
    {
        // This is an offline generated page; nothing is fetched except links explicitly
        // opened by the user. Character art is packaged locally with the application.
        string portraitPath = Path.Combine(AppContext.BaseDirectory, "Assets", "navi-character.svg");
        string portrait = "";
        try
        {
            var info = new FileInfo(portraitPath);
            if (info.Exists && info.Length is > 0 and < 1024 * 1024)
                portrait = "data:image/svg+xml;base64," +
                    Convert.ToBase64String(File.ReadAllBytes(portraitPath));
        }
        catch (Exception) { /* retain the compass fallback */ }

        string mark = AssetUri("navi-compass.svg");
        string logoMarkup = string.IsNullOrEmpty(mark)
            ? "<span aria-hidden=\"true\">✦</span>"
            : "<img src=\"" + mark + "\" alt=\"Navi Navigator compass logo\" />";

        string portraitMarkup = string.IsNullOrEmpty(portrait)
            ? """<div class="portrait-fallback" aria-label="Navi's compass emblem">✦</div>"""
            : "<img class=\"navi-portrait\" src=\"" + portrait +
              "\" alt=\"Navi the Navigator: teal, navy and white fox mascot\" />";

        string accent = WebUtility.HtmlEncode(skin.Accent);
        string soft = WebUtility.HtmlEncode(skin.AccentLight);
        string top = WebUtility.HtmlEncode(skin.Top);
        string bottom = WebUtility.HtmlEncode(skin.Bottom);
        string nav = WebUtility.HtmlEncode(skin.Name);
        string skinId = WebUtility.HtmlEncode(skin.Id);

        return $$"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta http-equiv="Content-Security-Policy"
  content="default-src 'none'; img-src data:; style-src 'unsafe-inline';">
<title>Navi Portal</title>
<style>
:root {
  --accent: {{accent}};
  --accent-soft: {{soft}};
  --chrome-top: {{top}};
  --chrome-bottom: {{bottom}};
}
* { box-sizing: border-box; }
body {
  margin: 0;
  min-height: 100vh;
  font: 14px 'Trebuchet MS', Tahoma, Verdana, sans-serif;
  color: #1c364b;
  background: linear-gradient(135deg,#d8e8ed,#edf7fa 50%,#dce7ef);
}
.topline {
  background:linear-gradient(180deg,#0e2b41,#0b4659 65%,#008087);
  color:#f6ffff;
  border-bottom:4px solid var(--accent-soft);
  padding:20px 26px 24px;
  display:flex;
  align-items:center;
  gap:15px;
}
.logo {
  display:grid;
  place-items:center;
  width:64px;
  height:64px;
  flex:0 0 64px;
  border:3px double #b9f1f0;
  border-radius:50%;
  background:radial-gradient(circle,#169ea2 30%,#083247);
  box-shadow:inset 0 1px 10px #0008,0 2px 6px #0006;
  font:48px Georgia,serif;
  line-height:1;
}
 .logo span { transform:translateY(-2px); }
.logo img { width:60px; height:60px; display:block; filter:drop-shadow(0 2px 3px #001b2b88); }
h1 { margin:0; font:italic bold 38px Georgia,serif; letter-spacing:-1.4px; text-shadow:1px 2px 3px #001b2b; }
.topline p { margin:5px 0 0; color:#abdedf; letter-spacing:3px; font-size:11px; text-transform:uppercase; }
.frame { display:grid; grid-template-columns:184px minmax(0,1fr); max-width:1420px; margin:0 auto; }
aside {
  background:linear-gradient(180deg,#d4e4ef,#b5ccdb);
  border-right:1px solid #91b0c1;
  padding:12px 10px;
  min-height:calc(100vh - 116px);
}
aside h2 { margin:10px 0 5px; padding:5px 7px; font-size:12px; color:#284c60;
  border:1px solid #8aabba; background:linear-gradient(#f7fcfe,#c5dce9); }
aside a { display:block; padding:7px 8px; color:#154c66; font-size:13px; text-decoration:none; border-radius:4px; }
aside a:hover { background:linear-gradient(var(--accent-soft),#d9f7f6); color:#082d42; }
.content { padding:24px 26px 34px; }
.welcome {
  position:relative; display:grid; grid-template-columns:230px minmax(0,1fr);
  gap:25px; min-height:294px; padding:18px;
  background:linear-gradient(140deg,#e3f2f6 15%,#e5f5f5 53%,#bad7e4);
  border:1px solid #9dbdc9;
  border-radius:8px;
  box-shadow:inset 0 2px #fff,0 3px 8px #45677724;
}
.portrait-card { display:flex; flex-direction:column; align-items:center; justify-content:center;
  background:radial-gradient(circle at 50% 35%,#dffbfa,#2c7e8e 73%,#143b56);
  border:3px solid #e6ffff; outline:1px solid #6a9aa8; border-radius:10px;
  overflow:hidden; box-shadow:0 3px 10px #123c4f44;
}
.navi-portrait { display:block; height:260px; max-width:100%; width:auto; object-fit:contain;
  filter:drop-shadow(0 4px 9px #051f3566);
}
.portrait-fallback { color:white; font:130px Georgia,serif; text-shadow:0 3px 18px #072638; }
.caption { margin-top:8px; padding:2px 10px; border-radius:30px; color:#e7ffff;
  font-size:12px; font-weight:bold; background:#06334b99; letter-spacing:.3px; }
.hero h2 { color:#163b54; margin:15px 0 5px; font:italic bold 29px Georgia,serif; }
.hero p { line-height:1.6; max-width:660px; }
.hero .slogan { font-weight:bold; font-size:16px; color:#007982; }
.wave { height:5px; border-radius:3px; background:linear-gradient(to right,var(--accent),#a4e7e4,transparent);
  margin:22px 0; }
.tiles { display:grid; grid-template-columns:repeat(4,minmax(145px,1fr)); gap:13px; margin-top:18px; }
.tiles a { color:#183d56; display:block; text-decoration:none; border:1px solid #93b3c2;
  border-radius:6px; background:linear-gradient(#f9ffff,#d2e4ed); padding:17px 13px 15px;
  box-shadow:inset 0 1px #fff,0 2px 3px #6082962e; min-height:124px; }
.tiles a:hover { background:linear-gradient(#f5ffff,#b6eeee); border-color:var(--accent); }
.tile-symbol { color:#007d85; font:32px Georgia,serif; display:block; margin-bottom:11px; }
.tile-title { font-weight:800; display:block; font-size:15px; }
.tile-note { display:block; font-size:12px; margin-top:6px; line-height:1.4; }
.info { margin-top:25px; border-top:1px solid #a4c3cd; padding:15px 3px 0; color:#3c6376; }
small { color:#5a7383; }
/* The portal responds to the actual loaded skin, not only its accent color. */
body[data-skin="classic"] { font-family:Tahoma,Verdana,sans-serif;
 background:#d9e3ea; }
body[data-skin="classic"] .topline { background:linear-gradient(#164e64,#306e86); }
body[data-skin="classic"] aside { background:linear-gradient(#e7edf1,#c3cfd8); }
body[data-skin="classic"] .welcome,body[data-skin="classic"] .tiles a {
 border-radius:1px; box-shadow:inset 1px 1px #fff,2px 2px #9cadb8; }
body[data-skin="classic"] .portrait-card { border-radius:2px; }
body[data-skin="fusion"] { color:#e6f8f8; background:linear-gradient(135deg,#13283e,#25556a); }
body[data-skin="fusion"] .topline { background:linear-gradient(#0a162c,#17455d 60%,#006c79); }
body[data-skin="fusion"] aside { background:linear-gradient(#19374d,#102a43);border-color:#56b0b5; }
body[data-skin="fusion"] aside h2 { color:#e1ffff; background:linear-gradient(#32687a,#164254);border-color:#177a83; }
body[data-skin="fusion"] aside a { color:#c5eff2; }
body[data-skin="fusion"] aside a:hover { background:#21747c; color:white; }
body[data-skin="fusion"] .welcome { color:#dbf8f8; border-color:#21848b;
 background:linear-gradient(125deg,#183a50,#2d6780 67%,#153a55); box-shadow:inset 0 1px #8bdbd878,0 3px 12px #020d1c; }
body[data-skin="fusion"] .hero h2 { color:#ecffff; }
body[data-skin="fusion"] .hero .slogan { color:#a8f6ed; }
body[data-skin="fusion"] .portrait-card { background:radial-gradient(circle at 45% 25%,#42a7aa,#102c47); outline-color:#167b87; }
body[data-skin="fusion"] .tiles a { color:#e7f8f8; border-color:#307d86;
 background:linear-gradient(#315e72,#173650); }
body[data-skin="fusion"] .tiles a:hover { background:linear-gradient(#347b84,#236372); }
body[data-skin="fusion"] .tile-symbol { color:#97eeee; }
body[data-skin="fusion"] .info,body[data-skin="fusion"] small { color:#a8d9df; }
body[data-skin="fusion"] .info { border-color:#397d88; }
@media(max-width:850px) {
  .frame { grid-template-columns:1fr; }
  aside { display:none; }
  .content {padding:16px;}
  .tiles { grid-template-columns:repeat(2,minmax(145px,1fr)); }
}
@media(max-width:500px) {
  .welcome { grid-template-columns:1fr; }
  .portrait-card { min-height:230px; }
  .topline {padding:14px;}
  h1 {font-size:28px;}
}
</style>
</head>
<body data-skin="{{skinId}}">
<header class="topline">
  <div class="logo">{{logoMarkup}}</div>
  <div><h1>Navi Navigator</h1><p>Charting new frontiers across the web</p></div>
</header>
<div class="frame">
  <aside>
    <h2>◈ Navi Services</h2>
    <a href="https://github.com/BlackShuck89/Navi-Navigator">Project Home</a>
    <a href="https://github.com/BlackShuck89/Navi-Navigator/releases">Downloads</a>
    <a href="https://github.com/BlackShuck89/Navi-Navigator/issues">Support &amp; Feedback</a>
    <h2>◈ Explore</h2>
    <a href="https://archive.org/">Internet Archive</a>
    <a href="https://en.wikipedia.org/">Wikipedia</a>
    <a href="https://www.mozilla.org/">Mozilla</a>
  </aside>
  <main class="content">
    <section class="welcome">
      <div class="portrait-card">
        {{portraitMarkup}}
        <div class="caption">Navi the Navigator</div>
      </div>
      <div class="hero">
        <h2>Welcome to the Navi Portal</h2>
        <p class="slogan">Home is where the link is.</p>
        <p>Meet Navi, the turquoise-and-navy fox who charts the open web. Familiar menus,
           expressive buttons, customizable interface skins, and the freedom
           to explore without the browser disappearing into minimalism.</p>
        <div class="wave"></div>
        <small>Appearance: {{nav}} &nbsp;·&nbsp; Technical preview · WebView2 backend</small>
      </div>
    </section>
    <section class="tiles">
      <a href="https://github.com/BlackShuck89/Navi-Navigator/releases">
        <span class="tile-symbol">↓</span><span class="tile-title">Downloads</span>
        <span class="tile-note">Get the latest test builds.</span>
      </a>
      <a href="https://github.com/BlackShuck89/Navi-Navigator">
        <span class="tile-symbol">✦</span><span class="tile-title">Project</span>
        <span class="tile-note">Navi's home on GitHub.</span>
      </a>
      <a href="https://archive.org/">
        <span class="tile-symbol">⌁</span><span class="tile-title">Web Archive</span>
        <span class="tile-note">Explore the internet's history.</span>
      </a>
      <a href="https://github.com/BlackShuck89/Navi-Navigator/issues">
        <span class="tile-symbol">?</span><span class="tile-title">Feedback</span>
        <span class="tile-note">Report bugs and suggest improvements.</span>
      </a>
    </section>
    <div class="info">Built for the future. Inspired by the web's adventurous past.<br>
      <small>Navi Navigator 0.0.3 preview · This page and its artwork are bundled locally with the browser.</small>
    </div>
  </main>
</div>
</body>
</html>
""";
    }
    /// <summary>A local About page with original Navi character and emblem art.</summary>
    internal static string BuildAboutHtml(NaviSkin skin)
    {
        string mascot = AssetUri("navi-character.svg");
        string mark = AssetUri("navi-compass.svg");
        string mainArt = string.IsNullOrEmpty(mascot)
            ? "<div class=\"fallback\">✦</div>"
            : "<img class=\"portrait\" src=\"" + mascot +
              "\" alt=\"Navi the Navigator, teal fox with white markings and compass jacket\">";
        string brandArt = string.IsNullOrEmpty(mark)
            ? "✦" : "<img class=\"mark\" src=\"" + mark + "\" alt=\"Navi Navigator compass emblem\">";
        string accent = WebUtility.HtmlEncode(skin.Accent);
        string skinName = WebUtility.HtmlEncode(skin.Name);
        return $$"""
<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline';">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>About Navi Navigator</title>
<style>
* {box-sizing:border-box} body {margin:0;font:13px 'Trebuchet MS',Tahoma,sans-serif;color:#153449;
background:linear-gradient(160deg,#f7fafc,#c8dfe8)}
header {display:flex;align-items:center;gap:13px;padding:17px 24px;
background:linear-gradient(#19364e,#096d7b);border-bottom:4px solid {{accent}};color:white}
.mark {height:64px;width:64px;filter:drop-shadow(1px 3px 3px #0006)}
h1{margin:0;font:bold italic 31px Georgia,serif}header p{margin:3px 0;color:#b8ecee;letter-spacing:2px;font-size:10px}
.frame{display:grid;grid-template-columns:190px 1fr;gap:22px;margin:20px;padding:12px;
border:1px solid #91b2c2;background:linear-gradient(#fff,#d8e9ef);box-shadow:inset 0 2px #fff,0 3px 7px #0e405222}
.left{background:linear-gradient(#b9ebea,#3b93a2 85%,#16374f);text-align:center;overflow:hidden;min-height:246px}
.portrait{width:100%;height:240px;object-fit:contain;filter:drop-shadow(0 3px 4px #001c3a66)}
.fallback{font:90px Georgia,serif;color:white;padding-top:60px}
h2{font:italic bold 23px Georgia,serif;margin:8px 0 11px}.body{line-height:1.65}
strong{color:#086b79} .label{display:inline-block;width:85px;color:#446678}
footer{text-align:center;color:#547386;font-size:11px;margin-bottom:12px}
</style></head>
<body><header>{{brandArt}}<div><h1>Navi Navigator</h1><p>CHARTING NEW FRONTIERS</p></div></header>
<div class="frame"><div class="left">{{mainArt}}</div>
<div class="body"><h2>About Navi Navigator</h2>
<p><strong>Version 0.0.3</strong> — Windows technical preview</p>
<p><span class="label">Appearance</span>{{skinName}}<br>
<span class="label">Rendering</span>Microsoft WebView2 (Chromium)<br>
<span class="label">Platform</span>Windows x64</p>
<p>Meet Navi, a turquoise fox explorer with white fur markings, navy paws and tail accents,
and a celestial globe-grid motif. His compass represents navigation and curiosity.</p>
<p>An independently branded, nostalgic desktop browser prototype inspired by the desktop-web era.
Firefox/Gecko integration is a separate ongoing development effort.</p>
<p><strong>Experimental software:</strong> no built-in auto-updates or code signing;
not recommended for sensitive browsing.</p></div></div>
<footer>Original Navi Navigator branding · Not affiliated with Netscape, Mozilla or Microsoft.</footer>
</body></html>
""";
    }

    private static string AssetUri(string filename)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", filename);
        try
        {
            var file = new FileInfo(path);
            if (file.Exists && file.Length is > 0 and < 1024 * 1024)
                return "data:image/svg+xml;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }
        catch (Exception) { /* Fallback if a packaged asset is missing. */ }
        return "";
    }

}
