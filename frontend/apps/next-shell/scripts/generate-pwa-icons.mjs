/**
 * generate-pwa-icons.mjs
 * Generates solid-color PNG icons for PWA using pure Node.js (zlib + Buffer).
 * No external dependencies required.
 */
import { createWriteStream, mkdirSync } from 'fs';
import { deflateSync } from 'zlib';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const publicDir = join(__dirname, '..', 'public', 'icons');

mkdirSync(publicDir, { recursive: true });

function crc32(buf) {
  let crc = 0xffffffff;
  for (const b of buf) {
    crc ^= b;
    for (let j = 0; j < 8; j++) crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function uint32BE(n) {
  return Buffer.from([n >>> 24, (n >>> 16) & 0xff, (n >>> 8) & 0xff, n & 0xff]);
}

function pngChunk(type, data) {
  const len = uint32BE(data.length);
  const typeBytes = Buffer.from(type, 'ascii');
  const crc = crc32(Buffer.concat([typeBytes, data]));
  return Buffer.concat([len, typeBytes, data, uint32BE(crc)]);
}

function generatePNG(width, height, r, g, b) {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;  // bit depth
  ihdr[9] = 2;  // color type: RGB
  ihdr[10] = 0; // compression
  ihdr[11] = 0; // filter
  ihdr[12] = 0; // interlace

  // Raw image data: each row starts with filter byte 0
  const rowSize = width * 3;
  const rawData = Buffer.alloc((rowSize + 1) * height);
  for (let y = 0; y < height; y++) {
    const off = y * (rowSize + 1);
    rawData[off] = 0; // filter none
    for (let x = 0; x < width; x++) {
      rawData[off + 1 + x * 3] = r;
      rawData[off + 1 + x * 3 + 1] = g;
      rawData[off + 1 + x * 3 + 2] = b;
    }
  }
  const compressed = deflateSync(rawData, { level: 9 });

  return Buffer.concat([
    signature,
    pngChunk('IHDR', ihdr),
    pngChunk('IDAT', compressed),
    pngChunk('IEND', Buffer.alloc(0)),
  ]);
}

const icons = [
  { file: 'icon-192.png',          w: 192,  h: 192,  r: 0x1e, g: 0x40, b: 0xaf },
  { file: 'icon-512.png',          w: 512,  h: 512,  r: 0x1e, g: 0x40, b: 0xaf },
  { file: 'icon-maskable-192.png', w: 192,  h: 192,  r: 0x1d, g: 0x4e, b: 0xd8 },
  { file: 'icon-maskable-512.png', w: 512,  h: 512,  r: 0x1d, g: 0x4e, b: 0xd8 },
];

for (const { file, w, h, r, g, b } of icons) {
  const png = generatePNG(w, h, r, g, b);
  const out = join(publicDir, file);
  const ws = createWriteStream(out);
  ws.write(png);
  ws.end();
  console.log(`✓ Generated ${out} (${w}x${h})`);
}
