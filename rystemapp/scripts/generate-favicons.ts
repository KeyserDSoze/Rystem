// Script to generate PNG favicons from SVG
// This is a placeholder script - requires sharp package to be installed
// Run: npm install --save-dev sharp

import sharp from 'sharp';
import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const svgPath = join(__dirname, '../public/favicon.svg');
const publicDir = join(__dirname, '../public');

const sizes = [
  { name: 'favicon-16x16.png', size: 16 },
  { name: 'favicon-32x32.png', size: 32 },
  { name: 'apple-touch-icon.png', size: 180 },
  { name: 'favicon-192x192.png', size: 192 },
  { name: 'favicon-512x512.png', size: 512 },
];

async function generateFavicons() {
  const svgBuffer = readFileSync(svgPath);

  console.log('🎨 Generating PNG favicons from SVG...\n');

  for (const { name, size } of sizes) {
    const outputPath = join(publicDir, name);
    await sharp(svgBuffer)
      .resize(size, size)
      .png()
      .toFile(outputPath);
    console.log(`✓ Generated ${name} (${size}x${size})`);
  }

  console.log('\n✅ All favicons generated successfully!');
}

generateFavicons().catch((err) => {
  console.error('❌ Error generating favicons:', err);
  process.exit(1);
});
