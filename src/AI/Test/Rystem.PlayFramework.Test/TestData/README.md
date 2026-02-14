# Test Data Files

This directory contains test files for multi-modal support testing.

## Files

- **sample.txt** - Plain text file
- **sample-data.json** - JSON data file
- **sample-image.png** - Mock PNG image (created programmatically in tests)
- **sample-audio.mp3** - Mock MP3 audio (created programmatically in tests)

## Usage

These files are used by `MultiModalTests.cs` to test:
- File input via `MultiModalInput.FromFileBytes()`
- Tool output with `DataContent`
- Multi-modal conversation with files