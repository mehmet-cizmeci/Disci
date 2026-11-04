// Kullanım kılavuzunun ekran görüntülerini uydurma verili örnek klinikten çeker (docs/kilavuz-resimler).
// Gerekenler: Node 22+, Google Chrome. Çalıştırma: powershell -File docs\ekran-goruntuleri.ps1
// Bu betik yalnızca http://localhost:5223 adresindeki örnek sunucuya bağlanır (tests/.../GuideScreenshotServer.cs).
import { spawn } from 'node:child_process';
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const outDir = join(here, 'kilavuz-resimler');
const base = 'http://localhost:5223';
const mode = process.argv[2] ?? 'hepsi';   // 'kurulum' or 'hepsi'
mkdirSync(outDir, { recursive: true });

const chromePath = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe']
  .find(existsSync);
if (!chromePath) throw new Error('Google Chrome bulunamadı.');
const profile = join(tmpdir(), 'disci-ekran-' + Date.now());
const chrome = spawn(chromePath, ['--headless=new', '--remote-debugging-port=9333', `--user-data-dir=${profile}`, '--hide-scrollbars',
  '--no-first-run', '--lang=tr-TR', 'about:blank'], { stdio: 'ignore' });

const sleep = ms => new Promise(r => setTimeout(r, ms));
let ws, seq = 0;
const pending = new Map();
function send(method, params = {}) {
  const id = ++seq;
  ws.send(JSON.stringify({ id, method, params }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}
async function connect() {
  for (let i = 0; i < 50; i++) {
    try {
      const list = await (await fetch('http://127.0.0.1:9333/json')).json();
      const page = list.find(t => t.type === 'page');
      if (page) { ws = new WebSocket(page.webSocketDebuggerUrl); break; }
    } catch { }
    await sleep(200);
  }
  await new Promise(r => ws.addEventListener('open', r, { once: true }));
  ws.addEventListener('message', e => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) { const p = pending.get(m.id); pending.delete(m.id); m.error ? p.reject(new Error(m.error.message)) : p.resolve(m.result); }
  });
  await send('Page.enable');
  await send('Runtime.enable');
  await send('Emulation.setDeviceMetricsOverride', { width: 1366, height: 860, deviceScaleFactor: 1, mobile: false });
}
const evaluate = async expr => (await send('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true })).result.value;
async function go(path, waitMs = 2500) {
  await send('Page.navigate', { url: base + path });
  for (let i = 0; i < 60 && (await evaluate('document.readyState')) !== 'complete'; i++) await sleep(100);
  await sleep(waitMs);   // Blazor bağlantısı ve veri yükleme
}
async function click(text, selector = 'button, a') {
  const ok = await evaluate(`(() => { const el = [...document.querySelectorAll(${JSON.stringify(selector)})]
    .find(e => e.textContent.trim().includes(${JSON.stringify(text)}) && e.offsetParent !== null);
    if (!el) return false; el.scrollIntoView({ block: 'center', behavior: 'instant' }); el.click(); return true; })()`);
  if (!ok) console.warn('  tıklanamadı:', text);
  await sleep(1500);
}
async function shot(name, fullPage = false) {
  await evaluate('window.scrollTo({ top: 0, behavior: "instant" })');
  let clip;
  if (fullPage) {
    const h = Math.min(await evaluate('document.documentElement.scrollHeight'), 2200);
    await send('Emulation.setDeviceMetricsOverride', { width: 1366, height: h, deviceScaleFactor: 1, mobile: false });
    await sleep(600);
  }
  const { data } = await send('Page.captureScreenshot', { format: 'png', clip });
  writeFileSync(join(outDir, name + '.png'), Buffer.from(data, 'base64'));
  if (fullPage) await send('Emulation.setDeviceMetricsOverride', { width: 1366, height: 860, deviceScaleFactor: 1, mobile: false });
  console.log('  ' + name + '.png');
}
async function signIn(role, name) {
  await send('Network.enable');
  await send('Network.setCookie', { name: 'test-role', value: role, url: base });
  await send('Network.setCookie', { name: 'test-name', value: name, url: base });
}

try {
  await connect();
  if (mode === 'kurulum') {
    await go('/kurulum', 1500);
    await evaluate(`(() => { const set = (id, v) => { const e = document.getElementById(id); if (e) e.value = v; };
      set('ClinicName', 'Örnek Ağız ve Diş Sağlığı Kliniği'); set('Address', 'Deneme Mah. Örnek Cad. No: 1, İstanbul');
      set('Phone', '0212 000 00 00'); set('FullName', 'Elif Örnek'); set('DentistName', 'Dt. Elif Örnek'); set('Email', 'yonetici@ornek.test'); })()`);
    await shot('01-kurulum', true);
  } else {
    const { patient } = JSON.parse(readFileSync(join(tmpdir(), 'disci-demo.json'), 'utf8'));
    await go('/Account/Login', 1200); await shot('02-giris');
    await signIn('Admin', 'yonetici@ornek.test');
    await go('/'); await shot('03-bugun');
    await go('/hastalar'); await shot('04-hastalar');
    await go(`/hastalar/${patient}`, 3000); await shot('05-hasta-karti');
    await click('Tedaviler', '.tabs button'); await shot('06-tedaviler', true);
    await go(`/hastalar/${patient}`, 3000); await click('Ödeme al'); await shot('07-odeme');
    await go('/randevular', 3500); await shot('08-randevular');
    await go('/bildirimler'); await shot('09-bildirimler');
    await go(`/hastalar/${patient}`, 3000); await click('Reçete yaz'); await sleep(800);
    await click('Çekim sonrası');
    await shot('10-recete');
    await go('/kasa?donem=ay', 3000); await shot('11-kasa');
    await go('/ayarlar'); await shot('12-ayarlar');
    await go('/yonetim/kullanicilar'); await shot('13-kullanicilar');
  }
} finally {
  writeFileSync(join(tmpdir(), 'disci-demo.stop'), 'stop');   // örnek sunucuyu kapatır
  try { ws?.close(); } catch { }
  chrome.kill();
}
