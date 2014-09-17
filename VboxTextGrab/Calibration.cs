using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace VboxTextGrab
{
    class Calibration
    {
        private Bitmap unicodePattern;
        private int lastUnicode = 0, lastLastUnicode = 0;

        public Calibration()
        {
            Status = "Calibrating...";
            IsFinished = false;
            WasSame = false;
        }

        public String Status { get; private set; }
        public bool IsFinished { get; private set; }
        public bool WasSame { get; private set; }

        public bool Add(Bitmap bmp)
        {
            IsFinished = WasSame = false;
            BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = data.Stride / 4;
            int[] rawData = new int[stride * data.Height];
            Marshal.Copy(data.Scan0, rawData, 0, rawData.Length);
            bmp.UnlockBits(data);
            if (unicodePattern != null)
            {
                return ParseUnicodePattern(bmp, rawData, stride, unicodePattern.Width / 256, unicodePattern.Height / 256);
            }
            else
            {
                return ParseBorderPattern(bmp, rawData, stride) || ParseCodepagePattern(bmp, rawData, stride);
            }
        }

        private bool ParseCodepagePattern(Bitmap bmp, int[] rawData, int stride)
        {
            int bgColor = rawData[0];
            int fgColor = ~bgColor, cols = -1, rows = -1;

            // find the first foreground pixel. Note that it is assumed that there are at least 10 lines and 32 characters per line.
            for (int y = 0; cols == -1 && y < bmp.Height / 10; y++)
            {
                int baseY = y * stride;
                for (int x = 0; cols == -1 && x < bmp.Width / 32; x++)
                {
                    int pixel = rawData[baseY + x];
                    if (pixel != bgColor)
                    {
                        fgColor = pixel;
                        cols = 0;
                        for (int w = x + 1; w < bmp.Width / 32; w++)
                        {
                            if (bmp.Width % w != 0)
                                continue;
                            bool match = true;
                            cols = bmp.Width / w;
                            // quick line check
                            for (int xx = 0; match && xx < cols; xx++)
                            {
                                if (rawData[baseY + xx * w + x] != fgColor)
                                    match = false;
                            }
                            // full line check
                            for (int ww = 0; match && ww < w; ww++)
                            {
                                for (int xx = 0; match && xx < cols; xx++)
                                {
                                    if (rawData[baseY + xx * w + ww] != rawData[baseY + ww])
                                        match = false;
                                }
                            }
                            if (match)
                                break;
                            cols = 0;
                        }
                        if (cols != 0)
                        {
                            for (int h = y; h <= bmp.Height / 10; h++)
                            {
                                if (bmp.Height % h != 0 || rawData[h * stride + x] != bgColor)
                                    continue;
                                bool match = true;
                                for (int yy = 0; match && yy < h; yy++)
                                {
                                    int baseY1 = yy * stride;
                                    int baseY2 = (yy + h) * stride;
                                    for (int xx = 0; match && xx < bmp.Width / cols; xx++)
                                    {
                                        if (rawData[baseY1 + xx] != rawData[baseY2 + xx])
                                            match = false;
                                    }
                                }
                                if (match)
                                {
                                    rows = bmp.Height / h;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (rows == -1)
            {
                Status = "Failed to detect calibration pattern";
                return false;
            }
            int cw = bmp.Width / cols, ch = bmp.Height / rows;
            // validate whole pattern again -- first two rows and first 32 chars of last row must be identical
            for (int i = 0; i < 3; i++)
            {
                int baseY = (i == 2 ? rows - 1 : i) * stride * ch;
                for (int j = 0; j < (i == 2 ? 32 : cols); j++)
                {
                    for (int y = 0; y < ch; y++)
                    {
                        int cmpPos = y * stride;
                        int basePos = baseY + j * cw + cmpPos;
                        for (int x = 0; x < cw; x++)
                        {
                            if (basePos == cmpPos)
                            {
                                if (rawData[basePos + x] != fgColor && rawData[basePos + x] != bgColor)
                                {
                                    Status = "Failed to validate calibration pattern - too many colors detected";
                                    return false;
                                }
                            }
                            if (rawData[basePos + x] != rawData[cmpPos + x])
                            {
                                Status = "Failed to validate calibration pattern - marker character missing or incorrect";
                                return false;
                            }
                        }
                    }
                }
            }
            for (int y = ch * 2; y < bmp.Height; y++)
            {
                int baseY = stride * y;
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (rawData[baseY + x] == bgColor)
                        continue;
                    int row = y / ch;
                    if (x > cw * 32 || row < 8 || (row >= 16 && row < rows - 1))
                    {
                        if (ParseUnicodePattern(bmp, rawData, stride, cw, ch))
                        {
                            unicodePattern = new Bitmap(cw * 256, ch * 256, PixelFormat.Format4bppIndexed);
                            lastUnicode = lastLastUnicode = 0;
                            ColorPalette palette = unicodePattern.Palette;
                            palette.Entries[0] = Color.Red;
                            palette.Entries[1] = Color.Blue;
                            palette.Entries[2] = Color.Yellow;
                            unicodePattern.Palette = palette;
                            return ParseUnicodePattern(bmp, rawData, stride, cw, ch);
                        }
                        return false;
                    }
                }
            }

            // save pattern
            Bitmap pattern = new Bitmap(cw * 32, ch * 8, PixelFormat.Format1bppIndexed);
            BitmapData patData = pattern.LockBits(new Rectangle(Point.Empty, pattern.Size), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] rawPatData = new byte[patData.Stride * patData.Height];

            for (int y = 0; y < ch * 8; y++)
            {
                int baseY = stride * (y + ch * 8);
                for (int x = 0; x < cw * 32; x++)
                {
                    byte col;
                    if (rawData[baseY + x] == bgColor)
                        col = 0x00;
                    else if (rawData[baseY + x] == fgColor)
                        col = 0xff;
                    else
                    {
                        Status = "Failed to validate calibration pattern - too many colors detected";
                        return false;
                    }
                    rawPatData[y * patData.Stride + x] = col;
                }
            }
            Marshal.Copy(rawPatData, 0, patData.Scan0, rawPatData.Length);
            pattern.UnlockBits(patData);
            pattern.Save(@"glyphmaps\calibrated-" + DateTime.Now.Ticks + "." + cols + "x" + rows + "." + bmp.Width + "x" + bmp.Height + ".glyphmap", ImageFormat.Png);
            Status = "Successfully calibrated: " + cols + "x" + rows;
            IsFinished = true;
            return true;
        }

        private bool ParseBorderPattern(Bitmap bmp, int[] rawData, int stride)
        {
            int bgColor = rawData[(bmp.Width - 1) + (bmp.Height - 1) * stride];
            int x1 = bmp.Width / 2, x2 = x1, y1 = bmp.Height / 2, y2 = y1;
            int fgColor = rawData[x1 + y1 * stride];
            // move borders
            while (x1 > 0 && rawData[(x1 - 1) + y1 * stride] == fgColor) x1--;
            while (y1 > 0 && rawData[x1 + (y1 - 1) * stride] == fgColor) y1--;
            while (x2 < bmp.Width - 1 && rawData[(x2 + 1) + y2 * stride] == fgColor) x2++;
            while (y2 < bmp.Height - 1 && rawData[x2 + (y2 + 1) * stride] == fgColor) y2++;
            // validate borders
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    int color = (x >= x1 && x <= x2 && y >= y1 && y <= y2) ? fgColor : bgColor;
                    if (rawData[x + y * stride] != color)
                        return false;
                }
            }

            // save pattern
            int w = x2 - x1 + 1, h = y2 - y1 + 1;
            string filename = @"glyphmaps\" + bmp.Width + "x" + bmp.Height + ".border";
            string newLine = x1 + " " + y1 + " " + w + " " + h;
            if (File.Exists(filename))
            {
                string[] oldLines = File.ReadAllLines(filename), newLines = new string[oldLines.Length + 1];
                newLines[0] = newLine;
                Array.Copy(oldLines, 0, newLines, 1, oldLines.Length);
                Dictionary<string, int> pixelSize = new Dictionary<string, int>();
                foreach (string line in newLines)
                {
                    string[] parts = line.Split(' ');
                    pixelSize[line] = int.Parse(parts[2]) * int.Parse(parts[3]);
                }
                Array.Sort(newLines, delegate(string s1, string s2)
                {
                    int ps1 = pixelSize[s1], ps2 = pixelSize[s2];
                    if (ps1 > ps2) return 1; else if (ps1 < ps2) return -1; else return 0;
                });
                File.WriteAllLines(filename, newLines);
            }
            else
            {
                File.WriteAllText(filename, newLine);
            }
            Status = "Successfully calibrated border pattern: " + w + "x" + h + " in " + bmp.Width + "x" + bmp.Height + " at " + x1 + "," + y1;
            IsFinished = true;
            return true;
        }

        private bool ParseUnicodePattern(Bitmap bmp, int[] rawData, int stride, int cw, int ch)
        {
            int cols = bmp.Width / cw, rows = bmp.Height / ch;
            int bgColor = rawData[0], fgColor = 42;

            // validate whole pattern again -- first two rows and first 32 chars of last row must be identical
            for (int i = 0; i < 3; i++)
            {
                int baseY = (i == 2 ? rows - 1 : i) * stride * ch;
                for (int j = 0; j < (i == 2 ? 32 : cols); j++)
                {
                    for (int y = 0; y < ch; y++)
                    {
                        int cmpPos = y * stride;
                        int basePos = baseY + j * cw + cmpPos;
                        for (int x = 0; x < cw; x++)
                        {
                            if (basePos == cmpPos)
                            {
                                if (rawData[basePos + x] != bgColor && fgColor == 42)
                                    fgColor = rawData[basePos + x];
                                if (rawData[basePos + x] != fgColor && rawData[basePos + x] != bgColor)
                                {
                                    Status = "Failed to validate calibration pattern - too many colors detected";
                                    return false;
                                }
                            }
                            if (rawData[basePos + x] != rawData[cmpPos + x])
                            {
                                Status = "Failed to validate calibration pattern - marker character missing or incorrect";
                                return false;
                            }
                        }
                    }
                }
            }
            for (int y = ch * 2; y < bmp.Height; y++)
            {
                int baseY = stride * y;
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (rawData[baseY + x] == bgColor)
                        continue;
                    else if (rawData[baseY + x] != fgColor)
                    {
                        Status = "Failed to validate calibration pattern - too many colors detected";
                    }
                    int row = y / ch;
                    if (x > cw * 74 || row == 2 || row == 4
                        || (row >= 23 && row < rows - 1)
                        || (row == 3 && x > cw * 23)
                        || (row >= 22 && row <= 24 && x > cw)
                        )
                    {
                        Status = "Failed to validate Unicode calibration pattern - characters outside allowed range";
                        return false;
                    }
                }
            }

            // validate line syntax
            GlyphKey plus = new GlyphKey(rawData, stride, 0, cw, ch);
            GlyphKey space = new GlyphKey(rawData, stride, 2 * ch * stride, cw, ch);
            foreach (int x in new[] { 0, 17, 22 })
            {
                GlyphKey check = new GlyphKey(rawData, stride, x * cw + 3 * ch * stride, cw, ch);
                if (!check.FullEquals(plus))
                {
                    Status = "Failed to validate Unicode calibration pattern - invalid digits line";
                    return false;
                }
            }
            foreach (int x in new[] { 4, 9 })
            {
                for (int y = 5; y < 21; y++)
                {
                    GlyphKey check = new GlyphKey(rawData, stride, x * cw + y * ch * stride, cw, ch);
                    if (!check.FullEquals(space))
                    {
                        Status = "Failed to validate Unicode calibration pattern - invalid data line";
                        return false;
                    }
                }
            }

            // parse header line
            Dictionary<GlyphKey, char> hexDigits = new Dictionary<GlyphKey, char>();
            for (int x = 0; x < 16; x++)
            {
                hexDigits.Add(new GlyphKey(rawData, stride, (x + 1) * cw + 3 * ch * stride, cw, ch), "0123456789ABCDEF"[x]);
            }
            int last = ParseHex(hexDigits, rawData, stride, 18 * cw + 3 * ch * stride, cw, ch);
            if (last == -1)
            {
                Status = "Failed to parse Unicode calibration pattern - invalid hex digits in header line";
                return false;
            }
            if (last != lastUnicode)
            {
                if (last == lastLastUnicode)
                {
                    WasSame = true;
                    return false;
                }
                Status = "Failed to parse Unicode calibration pattern - incorrect page order";
                return false;
            }
            lastLastUnicode = lastUnicode;

            // parse data lines
            bool moreScreens = new GlyphKey(rawData, stride, 21 * ch * stride, cw, ch).FullEquals(plus);
            for (int y = 5; y < 21; y++)
            {
                if (!moreScreens && new GlyphKey(rawData, stride, cw + y * ch * stride, cw, ch).FullEquals(space))
                {
                    StoreUnicodeGlyph(0, fgColor, bgColor, rawData, stride, y * ch * stride, cw, ch);
                    break;
                }
                int from = ParseHex(hexDigits, rawData, stride, y * ch * stride, cw, ch);
                int to = ParseHex(hexDigits, rawData, stride, 5 * cw + y * ch * stride, cw, ch);
                if (from == -1 || to == -1 || to < from || to >= from + 64)
                {
                    Status = "Failed to parse Unicode calibration pattern - invalid hex digits in data line";
                    return false;
                }
                for (int x = 0; x < to - from + 1; x++)
                {
                    StoreUnicodeGlyph(from + x, fgColor, bgColor, rawData, stride, (x + 10) * cw + y * ch * stride, cw, ch);
                }
                lastUnicode = to;
            }

            if (moreScreens)
            {
                Status = "Calibrated unicode pattern page for " + cols + "x" + rows + " - more pages pending";
                IsFinished = false;
            }
            else if (unicodePattern != null)
            {
                unicodePattern.Save(@"glyphmaps\unicode-calibrated-" + DateTime.Now.Ticks + "." + cols + "x" + rows + "." + bmp.Width + "x" + bmp.Height + ".glyphmap", ImageFormat.Png);
                Status = "Successfully calibrated Unicode pattern: " + cols + "x" + rows;
                IsFinished = true;
            }
            return true;
        }

        private int ParseHex(Dictionary<GlyphKey, char> hexDigits, int[] rawData, int stride, int offset, int cw, int ch)
        {
            char[] chars = new char[4];
            for (int i = 0; i < chars.Length; i++)
            {
                GlyphKey glyph = new GlyphKey(rawData, stride, offset + i * cw, cw, ch);
                if (!hexDigits.ContainsKey(glyph))
                    return -1;
                chars[i] = hexDigits[glyph];
            }
            return int.Parse(new string(chars), System.Globalization.NumberStyles.HexNumber);
        }

        private void StoreUnicodeGlyph(int charCode, int fgColor, int bgColor, int[] rawData, int stride, int offset, int cw, int ch)
        {
            if (unicodePattern == null)
                return;
            int xx = (charCode % 256) * cw;
            int yy = (charCode / 256) * ch;
            BitmapData patData = unicodePattern.LockBits(new Rectangle(xx, yy, cw, ch), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] rawPatData = new byte[patData.Stride * patData.Height];

            for (int y = 0; y < ch; y++)
            {
                int baseY = offset + y * stride;
                for (int x = 0; x < cw; x++)
                {
                    byte col;
                    if (rawData[baseY + x] == bgColor)
                        col = 0x01;
                    else if (rawData[baseY + x] == fgColor)
                        col = 0x02;
                    else
                    {
                        throw new Exception("Invalid color detected");
                    }
                    rawPatData[y * patData.Stride + x] = col;
                }
            }
            Marshal.Copy(rawPatData, 0, patData.Scan0, rawPatData.Length);
            unicodePattern.UnlockBits(patData);
        }
    }
}
