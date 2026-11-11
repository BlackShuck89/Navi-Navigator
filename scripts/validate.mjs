// Repository-only checks. Node is not part of Navi Navigator's browser runtime.
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (relative) => readFileSync(path.join(root, relative), 'utf8');
const manifest = JSON.parse(read('skins/manifest.json'));
assert.equal(manifest.schemaVersion, 1);
assert.equal(manifest.product, 'Navi Navigator');
assert.ok(Array.isArray(manifest.skins));
assert.ok(manifest.skins.length >= 3);

const ids = new Set();
const requiredKeys = ['surface', 'surfaceHigh', 'surfaceLow', 'accent', 'accentBright', 'text'];
for (const skin of manifest.skins) {
  assert.match(skin.id, /^[a-z][a-z0-9-]*$/);
  assert.ok(!ids.has(skin.id), 'Duplicate skin: ' + skin.id);
  ids.add(skin.id);
  assert.equal(skin.css, skin.id + '.css');
  assert.ok(skin.name && skin.description);
  for (const key of requiredKeys) {
    assert.match(skin.palette[key], /^#[0-9a-fA-F]{6}$/, skin.id + ' invalid color: ' + key);
  }
  checkCss('skins/' + skin.css);
}
assert.ok(ids.has(manifest.defaultSkin));
for (const id of ['modern', 'classic', 'fusion']) {
  assert.ok(ids.has(id));
}
checkCss('skins/base.css');
const base = read('skins/base.css');
for (const selector of ['#navigator-toolbox', '#nav-bar', '#TabsToolbar']) {
  assert.ok(base.includes(selector), 'Missing browser selector: ' + selector);
}
for (const rel of [
  'scripts/Start-NaviPreview.ps1',
  'config/mozconfig.artifact',
  'config/mozconfig.full',
  'config/upstream.json',
  'scripts/apply_gecko_overlay.py',
  'scripts/test_gecko_overlay.py',
  'scripts/Build-NaviGecko.sh',
  'docs/GECKO_BUILD.md',
  'docs/ARCHITECTURE.md',
  'docs/ROADMAP.md',
  'docs/THEMING.md',
  'LICENSE',
  '.github/workflows/ci.yml'
]) {
  assert.ok(existsSync(path.join(root, rel)), 'Missing: ' + rel);
}
const upstream = JSON.parse(read('config/upstream.json'));
assert.match(upstream.revision, /^[a-f0-9]{40}$/);
assert.ok(read('config/mozconfig.full').includes('browser/branding/navi'));
assert.ok(read('config/mozconfig.artifact').includes('--enable-artifact-builds'));
console.log('OK: ' + manifest.skins.length + ' Navi skins; manifest, CSS, and project structure validated.');

function checkCss(rel) {
  const text = read(rel);
  assert.ok(text.includes('{') && text.includes('}'), rel + ' is empty');
  // Structural check only; browser UI compatibility needs live visual testing.
  const stripped = text.replace(/\/\*[\s\S]*?\*\//g, '');
  let nesting = 0;
  for (const chr of stripped) {
    if (chr === '{') nesting++;
    if (chr === '}') nesting--;
    assert.ok(nesting >= 0, rel + ' has an unmatched closing brace');
  }
  assert.equal(nesting, 0, rel + ' has unmatched CSS braces');
  assert.ok(!text.includes('http://') && !text.includes('https://'), rel + ' must not load external CSS');
}
