const fs = require("fs");
const zlib = require("zlib");

function decodePNG(path) {
  const buf = fs.readFileSync(path);
  let pos = 8, w = 0, h = 0, bitDepth = 0, colorType = 0, interlace = 0;
  const idat = [];
  let palette = null, trns = null;
  while (pos < buf.length) {
    const len = buf.readUInt32BE(pos);
    const type = buf.toString("ascii", pos + 4, pos + 8);
    const data = buf.subarray(pos + 8, pos + 8 + len);
    if (type === "IHDR") {
      w = data.readUInt32BE(0); h = data.readUInt32BE(4);
      bitDepth = data[8]; colorType = data[9]; interlace = data[12];
    } else if (type === "PLTE") palette = data;
    else if (type === "tRNS") trns = data;
    else if (type === "IDAT") idat.push(data);
    else if (type === "IEND") break;
    pos += 12 + len;
  }
  if (bitDepth !== 8 || interlace !== 0) throw new Error(`unsupported PNG ${path} bd=${bitDepth} il=${interlace}`);
  const channels = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[colorType];
  if (!channels) throw new Error("unsupported color type " + colorType);
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * channels;
  const out = Buffer.alloc(w * h * 3);
  let prev = Buffer.alloc(stride);
  for (let y = 0; y < h; y++) {
    const ft = raw[y * (stride + 1)];
    const line = raw.subarray(y * (stride + 1) + 1, y * (stride + 1) + 1 + stride);
    const cur = Buffer.alloc(stride);
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? cur[x - channels] : 0;
      const b = prev[x];
      const c = x >= channels ? prev[x - channels] : 0;
      let v = line[x];
      switch (ft) {
        case 0: break;
        case 1: v = (v + a) & 255; break;
        case 2: v = (v + b) & 255; break;
        case 3: v = (v + ((a + b) >> 1)) & 255; break;
        case 4: {
          const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
          v = (v + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c)) & 255; break;
        }
        default: throw new Error("bad filter " + ft);
      }
      cur[x] = v;
    }
    for (let x = 0; x < w; x++) {
      const si = x * channels, di = (y * w + x) * 3;
      if (colorType === 0) { out[di] = out[di + 1] = out[di + 2] = cur[si]; }
      else if (colorType === 2) { out[di] = cur[si]; out[di + 1] = cur[si + 1]; out[di + 2] = cur[si + 2]; }
      else if (colorType === 3) { const p = cur[si] * 3; out[di] = palette[p]; out[di + 1] = palette[p + 1]; out[di + 2] = palette[p + 2]; }
      else if (colorType === 4) { out[di] = out[di + 1] = out[di + 2] = cur[si]; }
      else { out[di] = cur[si]; out[di + 1] = cur[si + 1]; out[di + 2] = cur[si + 2]; }
    }
    prev = cur;
  }
  return { w, h, rgb: out, colorType };
}

function analyze(path) {
  const img = decodePNG(path);
  const { w, h, rgb } = img;
  const hist = new Map();
  for (let i = 0; i < rgb.length; i += 3) {
    const k = (rgb[i] << 16) | (rgb[i + 1] << 8) | rgb[i + 2];
    hist.set(k, (hist.get(k) || 0) + 1);
  }
  const total = w * h;
  const top = [...hist.entries()].sort((a, b) => b[1] - a[1]).slice(0, 30)
    .map(([k, n]) => ({ hex: "#" + k.toString(16).padStart(6, "0"), n, pct: +(100 * n / total).toFixed(2) }));
  const bg = top[0].hex;
  return { img, hist, top, bg, total };
}

function hexToRgb(hex) { const n = parseInt(hex.slice(1), 16); return [n >> 16 & 255, n >> 8 & 255, n & 255]; }

module.exports = { decodePNG, analyze, hexToRgb };

if (require.main === module) {
  for (const f of process.argv.slice(2)) {
    const a = analyze(f);
    console.log(`\n=== ${f} ${a.img.w}x${a.img.h} bg=${a.bg} colors=${a.hist.size} ===`);
    for (const c of a.top) console.log(`${c.hex} ${c.pct}% (${c.n})`);
  }
}
