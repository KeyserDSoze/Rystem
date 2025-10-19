# Rystem Favicon

Red circle with white "R" letter favicon for Rystem Framework.

## Files

- `favicon.svg` - Main SVG favicon (100x100 base)
- `favicon-16x16.svg` - Small size optimized
- `favicon-32x32.svg` - Medium size optimized
- `manifest.json` - PWA manifest

## Design

- **Circle**: Red (#DC2626) with darker red border (#B91C1C)
- **Letter**: White (#FFFFFF) bold "R"
- **Theme Color**: #DC2626 (red-600 from Tailwind)

## Browser Support

Modern browsers support SVG favicons directly. The SVG file is lightweight and scales perfectly at any size.

## Generating PNG Versions (Optional)

If you need PNG versions for older browsers or specific platforms:

### Option 1: Using Online Tools
1. Open `favicon.svg` in browser
2. Use online tools like https://realfavicongenerator.net/
3. Upload the SVG and generate all sizes

### Option 2: Using ImageMagick (Command Line)
```bash
# Install ImageMagick first
magick convert -background none -density 1200 -resize 16x16 favicon.svg favicon-16x16.png
magick convert -background none -density 1200 -resize 32x32 favicon.svg favicon-32x32.png
magick convert -background none -density 1200 -resize 180x180 favicon.svg apple-touch-icon.png
magick convert -background none -density 1200 -resize 192x192 favicon.svg favicon-192x192.png
magick convert -background none -density 1200 -resize 512x512 favicon.svg favicon-512x512.png
```

### Option 3: Using Node.js Sharp
```bash
npm install --save-dev sharp
tsx scripts/generate-favicons.ts
```

## Testing

After deploying, test the favicon at:
- https://rystem.net/favicon.svg
- Check browser tab icon
- Check bookmark icon
- Test on mobile devices (home screen icon)
