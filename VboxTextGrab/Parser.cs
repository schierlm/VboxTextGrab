using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VboxTextGrab
{
    static class Parser
    {
        private static char[] charMap;
        static Parser()
        {
            byte[] bytes = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                bytes[i] = (byte)i;
            }
            charMap = Encoding.GetEncoding(437).GetChars(bytes);
            string controlCharShapes = "\0\u263A\u263B\u2665\u2666\u2663\u2660\u2022\u25D8\u25CB\u25D9\u2642\u2640\u266A\u266B\u263C\u25BA\u25C4\u2195\u203C\u00B6\u00A7\u25AC\u21A8\u2191\u2193\u2192\u2190\u221F\u2194\u25B2\u25BC";
            for (int i = 0; i < controlCharShapes.Length; i++)
            {
                charMap[i] = controlCharShapes[i];
            }
        }

        public static string Parse(Bitmap bmp, ColorInformation info)
        {
            int bestErrors = int.MaxValue, tmp;
            string bestResult = "";
            Bitmap bestGlyphMap = null;
            int[] bestScreenSize = null;

            BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int bmpStride = data.Stride / 4;
            int[] bmpData = new int[bmpStride * data.Height];
            Marshal.Copy(data.Scan0, bmpData, 0, bmpData.Length);
            bmp.UnlockBits(data);

            foreach (string mapFile in Directory.GetFiles("glyphmaps", "*." + bmp.Width + "x" + bmp.Height + ".glyphmap"))
            {
                Bitmap glyphMap = new Bitmap(mapFile);
                int parseErrors;
                int[] screenSize = new int[2];
                string result = Parse(bmp, bmpData, bmpStride, glyphMap, out parseErrors, screenSize, null, null, out tmp, out tmp);
                if (parseErrors < bestErrors)
                {
                    bestErrors = parseErrors;
                    bestResult = result;
                    if (bestGlyphMap != null) bestGlyphMap.Dispose();
                    bestGlyphMap = glyphMap;
                    bestScreenSize = screenSize;
                    if (parseErrors == 0) break;
                }
                else
                {
                    glyphMap.Dispose();
                }
            }
            string message = "";
            if (bestErrors == int.MaxValue)
            {
                message = "Grabbing failed. Maybe you need to calibrate the font first?";
            }
            else if (bestErrors != 0)
            {
                message = "*** WARNING: " + bestErrors + " characters could not be detected ***\n\n";
            }
            if (info != null && bestGlyphMap != null)
            {
                info.MessageLength = message.Length;
                info.ScreenWidth = bestScreenSize[0];
                int height = bestScreenSize[1];
                info.ForegroundColors = new Color[info.ScreenWidth * height];
                info.BackgroundColors = new Color[info.ScreenWidth * height];
                Parse(bmp, bmpData, bmpStride, bestGlyphMap, out bestErrors, bestScreenSize, info.ForegroundColors, info.BackgroundColors, out info.CursorX, out info.CursorY);
            }
            if (bestGlyphMap != null)
                bestGlyphMap.Dispose();
            return message + bestResult;
        }

        private static string Parse(Bitmap bmp, int[] bmpData, int bmpStride, Bitmap glyphMap, out int parseErrors, int[] screenSize, Color[] fgColors, Color[] bgColors, out int cursorX, out int cursorY)
        {
            int cw, ch;
            GlyphKey cursorGlyph;
            Dictionary<GlyphKey, string> glyphDictionary = ParseGlyphMap(glyphMap, out cw, out ch, out cursorGlyph);

            int width = bmp.Width / cw;
            int height = bmp.Height / ch;
            screenSize[0] = width;
            screenSize[1] = height;
            cursorX = -1; cursorY = -1;
            int fg = 0, bg = 0;
            StringBuilder result = new StringBuilder();
            parseErrors = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool found = false;
                    for (int i = 0; !found && i < (cursorX == -1 ? 2 : 1); i++)
                    {
                        GlyphKey glyph = new GlyphKey(bmpData, bmpStride, x * cw + bmpStride * y * ch, cw, ch);
                        if (glyph.Foreground == 42)
                            continue;
                        if (i == 1)
                        {
                            glyph = new GlyphKey(glyph, cursorGlyph);
                        }
                        string value;
                        if (glyphDictionary.TryGetValue(glyph, out value))
                        {
                            if (i == 1)
                            {
                                cursorX = x;
                                cursorY = y;
                            }
                            found = true;
                            result.Append(value[1]);
                            if (value[0] == 'N')
                            {
                                fg = glyph.Foreground;
                                bg = glyph.Background;
                            }
                            else
                            {
                                fg = glyph.Background;
                                bg = glyph.Foreground;
                            }
                        }
                    }
                    if (!found)
                    {
                        result.Append("\uFFFD");
                        fg = Color.Red.ToArgb();
                        bg = Color.White.ToArgb();
                        parseErrors++;
                    }
                    if (fgColors != null)
                    {
                        fgColors[x + width * y] = Color.FromArgb(fg);
                    }
                    if (bgColors != null)
                    {
                        bgColors[x + width * y] = Color.FromArgb(bg);
                    }
                }
                result.Append("\n");
            }
            return result.ToString();
        }

        private static Dictionary<GlyphKey, string> ParseGlyphMap(Bitmap glyphMap, out int cw, out int ch, out GlyphKey cursorGlyph)
        {
            Dictionary<GlyphKey, string> glyphDictionary = new Dictionary<GlyphKey, string>();
            BitmapData data = glyphMap.LockBits(new Rectangle(Point.Empty, glyphMap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int glyphMapStride = data.Stride / 4;
            int[] glyphMapData = new int[glyphMapStride * data.Height];
            Marshal.Copy(data.Scan0, glyphMapData, 0, glyphMapData.Length);
            glyphMap.UnlockBits(data);
            int redARGB = Color.Red.ToArgb();
            bool isUnicodeMap = glyphMapData[0] == redARGB || glyphMapData[0] == Color.Blue.ToArgb() || glyphMapData[0] == Color.Yellow.ToArgb();
            int ww = isUnicodeMap ? 256 : 32, hh = isUnicodeMap ? 256 : 8;
            cw = glyphMap.Width / ww;
            ch = glyphMap.Height / hh;
            cursorGlyph = new GlyphKey(glyphMapData, glyphMapStride, 0, cw, ch);
            for (int y = 0; y < hh; y++)
            {
                for (int x = 0; x < ww; x++)
                {
                    if ((x == 0 && y == 0) || (!isUnicodeMap && x == 31 && y == 7) || (isUnicodeMap && glyphMapData[x * cw + glyphMapStride * y * ch] == redARGB))
                        continue;
                    GlyphKey charGlyph = new GlyphKey(glyphMapData, glyphMapStride, x * cw + glyphMapStride * y * ch, cw, ch);
                    if (!glyphDictionary.ContainsKey(charGlyph))
                    {
                        char chr = isUnicodeMap ? (char)(x + 256 * y) : charMap[x + 32 * y];
                        glyphDictionary.Add(charGlyph, (charGlyph.Background == cursorGlyph.Background ? "N" : "R") + chr);
                    }
                }
            }
            return glyphDictionary;
        }

        internal static Bitmap RemoveBorder(Bitmap bmp)
        {
            string file = @"glyphmaps\" + bmp.Width + "x" + bmp.Height + ".border";
            if (File.Exists(file))
            {
                BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int bmpStride = data.Stride / 4;
                int[] bmpData = new int[bmpStride * data.Height];
                Marshal.Copy(data.Scan0, bmpData, 0, bmpData.Length);
                bmp.UnlockBits(data);
                int bgColor = bmpData[(bmp.Width - 1) + (bmp.Height - 1) * bmpStride];
                foreach (string line in File.ReadAllLines(file))
                {
                    string[] parts = line.Split(' ');
                    int xx = int.Parse(parts[0]), yy = int.Parse(parts[1]), ww = int.Parse(parts[2]), hh = int.Parse(parts[3]);
                    bool ok = true;
                    for (int x = 0; ok && x < bmp.Width; x++)
                    {
                        for (int y = 0; ok && y < bmp.Height; y++)
                        {
                            if (x >= xx && x < xx + ww && y >= yy && y < yy + hh)
                                continue;
                            if (bmpData[x + y * bmpStride] != bgColor)
                            {
                                ok = false;
                            }
                        }
                    }
                    if (ok)
                    {
                        Bitmap result = new Bitmap(ww, hh);
                        using (Graphics g = Graphics.FromImage(result))
                        {
                            g.DrawImage(bmp, -xx, -yy);
                        }
                        return result;
                    }
                }
            }
            return bmp;
        }
    }

    class ColorInformation
    {
        public int MessageLength;
        public int ScreenWidth;
        public int CursorX, CursorY;
        public Color[] ForegroundColors;
        public Color[] BackgroundColors;
    }

    class GlyphKey
    {
        private bool[] data;
        private int hashCode;

        public GlyphKey(int[] pixels, int stride, int offset, int cw, int ch)
        {
            Background = pixels[offset]; Foreground = 1;
            hashCode = 1;
            data = new bool[cw * ch];
            for (int y = 0; y < ch; y++)
            {
                int o = offset + y * stride;
                for (int x = 0; x < cw; x++)
                {
                    int pixel = pixels[o + x];
                    if (pixel != Background && pixel != Foreground)
                    {
                        Foreground = (Foreground == 1) ? pixel : 42;
                    }
                    data[y * cw + x] = pixel != Background;
                    hashCode = unchecked(hashCode * 31 + (data[y * cw + x] ? 1231 : 1237));
                }
            }
            if (Foreground == 1)
                Foreground = Background;
        }

        public GlyphKey(GlyphKey glyph, GlyphKey cursor)
        {
            Background = glyph.Background;
            Foreground = glyph.Foreground;
            data = new bool[glyph.data.Length];
            hashCode = 1;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = glyph.data[i] ^ cursor.data[i];
                hashCode = unchecked(hashCode * 31 + (data[i] ? 1231 : 1237));
            }
        }

        public int Foreground { get; private set; }
        public int Background { get; private set; }

        public bool FullEquals(GlyphKey other)
        {
            return Foreground == other.Foreground && Background == other.Background && Equals(other);
        }

        public override bool Equals(object obj)
        {
            GlyphKey other = obj as GlyphKey;
            if (other == null || other.data.Length != data.Length)
                return false;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != other.data[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}