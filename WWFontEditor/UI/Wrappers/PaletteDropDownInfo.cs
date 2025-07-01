﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;

namespace Nyerguds.Util.UI.Wrappers
{
    public class PaletteDropDownInfo
    {
        private const String INI_SECTION = "Palette";

        public String Name { get; set; }
        public Color[] Colors { get; set; }
        public Color[] ColorBackup { get; private set; }
        public Int32 BitsPerPixel { get; private set; }
        public String SourceFile { get; private set; }
        public Int32 Entry { get; set; }
        public Boolean PrefixIndex { get; set; }
        public Boolean SuffixSource { get; set; }
        
        public PaletteDropDownInfo(String name, Int32 bpp, Color[] colors, String sourceFile, Int32 entry, Boolean prefixIndex, Boolean suffixSource)
        {
            this.Name = name;
            this.BitsPerPixel = bpp;
            Int32 expectedcolors = 1 << bpp;
            Color[] palette = new Color[expectedcolors];
            Int32 copiedColors = Math.Min(colors.Length, expectedcolors);
            Array.Copy(colors, palette, copiedColors);
            for (Int32 i = copiedColors; i < expectedcolors; ++i)
                palette[i] = Color.Black;
            this.Colors = palette;
            this.ColorBackup = palette.ToArray();
            this.SourceFile = sourceFile;
            this.Entry = entry;
            this.PrefixIndex = prefixIndex;
            this.SuffixSource = suffixSource;
        }
        
        public Boolean IsChanged()
        {
            if (this.ColorBackup == null)
                return false;
            if (this.ColorBackup.Length != this.Colors.Length)
                return true;
            for (Int32 i = 0; i < this.Colors.Length; ++i)
            {
                if (this.ColorBackup[i].ToArgb() != this.Colors[i].ToArgb())
                    return true;
            }
            return false;
        }

        public void Revert()
        {
            Array.Copy(this.ColorBackup, this.Colors, this.Colors.Length);
        }

        public void ClearRevert()
        {
            Array.Copy(this.Colors, this.ColorBackup, this.Colors.Length);
        }

        public override String ToString()
        {
            String name = String.Empty;
            if (this.PrefixIndex)
                name += this.Entry.ToString("D2") + " ";
            name += this.Name;
            if (this.SuffixSource)
                name += " (" + this.SourceFile + " #" + this.Entry + ")";
            return name;
        }

        public static List<PaletteDropDownInfo> LoadSubPalettesInfoFromPalette(FileInfo file, Boolean listAll, Boolean prefixIndex, Boolean suffixSource)
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            try
            {
                if (file.Length != 0x300)
                    return palettes;
                // Treat as C&C 6-bit colour palette
                ColorSixBit[] pal = ColorUtils.ReadSixBitPaletteFile(file.FullName);
                Color[] fullPal = ColorUtils.GetEightBitColorPalette(pal);

                String bareName = file.Name;
                String inipath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(bareName)) + ".ini";
                if (File.Exists(inipath))
                {
                    IniFile paletteConfig = new IniFile(inipath);
                    for (Int32 i = 0; i < 16; ++i)
                    {
                        String name = paletteConfig.GetStringValue(INI_SECTION, i.ToString(), null);
                        Boolean hasName = !String.IsNullOrEmpty(name);
                        if (!hasName)
                            name = null;
                        if (listAll && !hasName)
                            name = String.Empty;
                        if (name == null)
                            continue;
                        Color[] subPalette = new Color[16];
                        Array.Copy(fullPal, i * 16, subPalette, 0, 16);
                        //if (subPalette.All(x => x.R == 0 && x.G == 0 && x.B == 0))
                        //    subPalette = ImageUtils.GenerateRainbowPalette(4, false, true, true, false).Entries;
                        //subPalette[0] = Color.FromArgb(0x00, subPalette[0]);
                        palettes.Add(new PaletteDropDownInfo(name, 4, subPalette, bareName, i, prefixIndex, suffixSource));
                    }
                }
                else
                {
                    // add as one 256 colour palette
                    //fullPal[0] = Color.FromArgb(0x00, fullPal[0]);
                    palettes.Add(new PaletteDropDownInfo(bareName, 8, fullPal, bareName, 0, false, false));
                }
            }
            catch { /* ignore and continue */ }
            return palettes;
        }
    }
}
