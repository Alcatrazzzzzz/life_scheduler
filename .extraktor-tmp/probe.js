const { decodePNG } = require("./png.js");

function load(path) { const img = decodePNG(path); return img; }

function hex(r, g, b) { return "#" + [r, g, b].map(v => v.toString(16).padStart(2, "0")).join(""); }

function colors(img, x0, y0, x1, y1) {
  const { w, h, rgb } = img;
  x0 = Math.max(0, x0); y0 = Math.max(0, y0); x1 = Math.min(w, x1); y1 = Math.min(h, y1);
  const m = new Map(); let n = 0;
  for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
    const i = (y * w + x) * 3;
    const k = hex(rgb[i], rgb[i + 1], rgb[i + 2]);
    m.set(k, (m.get(k) || 0) + 1); n++;
  }
  return [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, 12).map(([c, v]) => `${c} ${(100 * v / n).toFixed(1)}%`);
}

// Bounding box + connected components of pixels matching a color (exact or within tol)
function components(img, target, tol = 0) {
  const { w, h, rgb } = img;
  const [tr, tg, tb] = target;
  const mask = new Uint8Array(w * h);
  for (let i = 0, p = 0; i < rgb.length; i += 3, p++) {
    if (Math.abs(rgb[i] - tr) <= tol && Math.abs(rgb[i + 1] - tg) <= tol && Math.abs(rgb[i + 2] - tb) <= tol) mask[p] = 1;
  }
  const seen = new Uint8Array(w * h);
  const comps = [];
  const stack = new Int32Array(w * h);
  for (let p = 0; p < w * h; p++) {
    if (!mask[p] || seen[p]) continue;
    let sp = 0; stack[sp++] = p; seen[p] = 1;
    let minx = w, maxx = 0, miny = h, maxy = 0, area = 0;
    while (sp) {
      const q = stack[--sp];
      const x = q % w, y = (q / w) | 0;
      area++;
      if (x < minx) minx = x; if (x > maxx) maxx = x;
      if (y < miny) miny = y; if (y > maxy) maxy = y;
      if (x > 0 && mask[q - 1] && !seen[q - 1]) { seen[q - 1] = 1; stack[sp++] = q - 1; }
      if (x < w - 1 && mask[q + 1] && !seen[q + 1]) { seen[q + 1] = 1; stack[sp++] = q + 1; }
      if (y > 0 && mask[q - w] && !seen[q - w]) { seen[q - w] = 1; stack[sp++] = q - w; }
      if (y < h - 1 && mask[q + w] && !seen[q + w]) { seen[q + w] = 1; stack[sp++] = q + w; }
    }
    comps.push({ x: minx, y: miny, w: maxx - minx + 1, h: maxy - miny + 1, area });
  }
  return comps.sort((a, b) => b.area - a.area);
}

function runs(img, y, minLen = 20) {
  const { w, rgb } = img;
  const out = []; let start = 0; let cur = null;
  for (let x = 0; x < w; x++) {
    const i = (y * w + x) * 3; const c = hex(rgb[i], rgb[i + 1], rgb[i + 2]);
    if (c !== cur) {
      if (cur !== null && x - start >= minLen) out.push(`${cur} x${start}-${x - 1} (${x - start})`);
      cur = c; start = x;
    }
  }
  if (w - start >= minLen) out.push(`${cur} x${start}-${w - 1} (${w - start})`);
  return out;
}

function vruns(img, x, minLen = 20) {
  const { w, h, rgb } = img;
  const out = []; let start = 0; let cur = null;
  for (let y = 0; y < h; y++) {
    const i = (y * w + x) * 3; const c = hex(rgb[i], rgb[i + 1], rgb[i + 2]);
    if (c !== cur) {
      if (cur !== null && y - start >= minLen) out.push(`${cur} y${start}-${y - 1} (${y - start})`);
      cur = c; start = y;
    }
  }
  if (h - start >= minLen) out.push(`${cur} y${start}-${h - 1} (${h - start})`);
  return out;
}

// radius: for a solid rect, inset of fill at row y0+k vs row y0+hHalf
function radius(img, x0, y0, x1, y1, target) {
  const { w, rgb } = img;
  const [tr, tg, tb] = target;
  const match = (x, y) => { const i = (y * w + x) * 3; return Math.abs(rgb[i] - tr) <= 2 && Math.abs(rgb[i + 1] - tg) <= 2 && Math.abs(rgb[i + 2] - tb) <= 2; };
  const rowLeft = (y) => { for (let x = x0; x <= x1; x++) if (match(x, y)) return x; return -1; };
  const mid = Math.floor((y0 + y1) / 2);
  const leftMid = rowLeft(mid);
  const out = [];
  for (let k = 0; k <= 12; k++) {
    const l = rowLeft(y0 + k);
    out.push({ k, inset: l < 0 ? null : l - leftMid });
  }
  return out;
}

if (process.argv[2]==="lines"){const bg=(process.argv[8]||"#000000").replace("#","").match(/../g).map(h=>parseInt(h,16));const im=load(process.argv[3]);console.log(JSON.stringify(inkLines(im,+process.argv[4],+process.argv[5],+process.argv[6],+process.argv[7],bg,+(process.argv[9]||12))));process.exit(0);}
const cmd = process.argv[2], path = process.argv[3];
const img = load(path);
const colorArg = cmd === "radius" ? process.argv[4] : process.argv[4];
const parsed = colorArg && /^#?[0-9a-fA-F]{6}$/.test(colorArg) ? colorArg.replace("#","").match(/../g).map(h => parseInt(h, 16)) : [0,0,0];
const [tr, tg, tb] = parsed;
if (cmd === "colors") {
  console.log(colors(img, +process.argv[4], +process.argv[5], +process.argv[6], +process.argv[7]));
} else if (cmd === "comps") {
  const cs = components(img, [tr, tg, tb], +(process.argv[5] || 0));
  console.log(cs.slice(0, 40).map(c => `x${c.x} y${c.y} ${c.w}x${c.h} area=${c.area}`).join("\n"));
} else if (cmd === "row") {
  console.log(runs(img, +process.argv[4], +(process.argv[5] || 20)).join("\n"));
} else if (cmd === "col") {
  console.log(vruns(img, +process.argv[4], +(process.argv[5] || 20)).join("\n"));
} else if (cmd === "radius") {
  console.log(radius(img, +process.argv[5], +process.argv[6], +process.argv[7], +process.argv[8], [tr, tg, tb]));
}

function inkLines(img, x0, y0, x1, y1, bg, thr = 12) {
  const { w, rgb } = img;
  const rows = [];
  for (let y = y0; y < y1; y++) {
    let cnt = 0, minx = w, maxx = -1;
    for (let x = x0; x < x1; x++) {
      const i = (y * w + x) * 3;
      if (Math.abs(rgb[i]-bg[0]) > thr || Math.abs(rgb[i+1]-bg[1]) > thr || Math.abs(rgb[i+2]-bg[2]) > thr) { cnt++; if (x<minx) minx=x; if (x>maxx) maxx=x; }
    }
    rows.push({ y, cnt, minx, maxx });
  }
  const lines = []; let cur = null;
  for (const r of rows) {
    if (r.cnt > 0) { if (!cur) cur = { y0: r.y, y1: r.y, minx: r.minx, maxx: r.maxx }; else { cur.y1 = r.y; cur.minx = Math.min(cur.minx, r.minx); cur.maxx = Math.max(cur.maxx, r.maxx); } }
    else if (cur) { lines.push(cur); cur = null; }
  }
  if (cur) lines.push(cur);
  return lines.map(l => ({ y: l.y0, h: l.y1 - l.y0 + 1, x: l.minx, w: l.maxx - l.minx + 1 }));
}
