const { decodePNG } = require("./png.js");

function render(path, cols = 150) {
  const { w, h, rgb } = decodePNG(path);
  const bgR = rgb[0], bgG = rgb[1], bgB = rgb[2]; // first pixel assumed bg
  const bgLum = 0.2126 * bgR + 0.7152 * bgG + 0.0722 * bgB;
  const rows = Math.max(8, Math.round(cols * (h / w) * 0.5));
  const cw = w / cols, ch = h / rows;
  const lines = [];
  const accents = [];
  for (let ry = 0; ry < rows; ry++) {
    let line = "";
    for (let cx = 0; cx < cols; cx++) {
      const x0 = Math.floor(cx * cw), x1 = Math.min(w, Math.ceil((cx + 1) * cw));
      const y0 = Math.floor(ry * ch), y1 = Math.min(h, Math.ceil((ry + 1) * ch));
      let sr = 0, sg = 0, sb = 0, n = 0, maxSat = 0, satR = 0, satG = 0, satB = 0;
      for (let y = y0; y < y1; y += 2) for (let x = x0; x < x1; x += 2) {
        const i = (y * w + x) * 3;
        const r = rgb[i], g = rgb[i + 1], b = rgb[i + 2];
        sr += r; sg += g; sb += b; n++;
        const mx = Math.max(r, g, b), mn = Math.min(r, g, b), s = mx - mn;
        if (s > maxSat) { maxSat = s; satR = r; satG = g; satB = b; }
      }
      const r = sr / n, g = sg / n, b = sb / n;
      const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
      const d = Math.abs(lum - bgLum);
      let c;
      if (maxSat > 45) { c = "O"; accents.push({ x: Math.round((x0 + x1) / 2), y: Math.round((y0 + y1) / 2), hex: "#" + [satR,satG,satB].map(v=>v.toString(16).padStart(2,"0")).join("") }); }
      else if (d < 1.5) c = " ";
      else if (d < 4) c = ".";
      else if (d < 9) c = ":";
      else if (d < 20) c = "-";
      else if (d < 40) c = "=";
      else if (d < 70) c = "+";
      else c = "#";
      line += c;
    }
    lines.push(line);
  }
  return { lines, accents, w, h, rows, cols, pxPerCol: cw, pxPerRow: ch };
}

if (require.main === module) {
  const path = process.argv[2];
  const cols = +(process.argv[3] || 150);
  const r = render(path, cols);
  console.log(r.lines.map((l, i) => String(i).padStart(3) + "|" + l).join("\n"));
  console.log("px/col=" + r.pxPerCol.toFixed(1), "px/row=" + r.pxPerRow.toFixed(1));
  const uniq = new Map();
  for (const a of r.accents) uniq.set(a.hex, (uniq.get(a.hex) || 0) + 1);
  console.log("accent cells:", [...uniq.entries()].sort((a,b)=>b[1]-a[1]).slice(0,8));
}
