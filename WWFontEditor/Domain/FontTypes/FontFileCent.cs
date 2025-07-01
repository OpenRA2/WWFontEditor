﻿using System;
using System.Linq;
using Nyerguds.Util;
using Nyerguds.ImageManipulation;

namespace WWFontEditor.Domain.FontTypes
{
    public class FontFileCent : FontFile
    {
        public override Int32 SymbolsTypeMin { get { return 0x80; } }
        public override Int32 SymbolsTypeMax { get { return 0x80; } }
        public override Int32 SymbolsTypeFirst { get { return 0x20; } }
        public override Int32 FontHeightTypeMax { get { return 0xFF; } }
        public override Int32 FontWidthTypeMin { get { return 0x08; } }
        public override Int32 FontWidthTypeMax { get { return 0x08; } }
        public override Int32 YOffsetTypeMax { get { return 0xFF; } }
        /// <summary> Set this to False if individual symbols cannot have different sizes than their parent font.</summary>
        public override Boolean CustomSymbolWidthsForType { get { return true; } }
        /// <summary> Set this to False if individual symbols cannot have different sizes than their parent font.</summary>
        public override Boolean CustomSymbolHeightsForType { get { return false; } }
        /// <summary>Padding at the bottom of the font. Only used for the preview function.</summary>
        public override Int32 FontTypePaddingVertical { get { return 2; } }
        /// <summary>Padding between the characters of the font. Only used for the preview function.</summary>
        public override Int32 FontTypePaddingHorizontal { get { return 1; } }
        public override Int32 BitsPerPixel { get { return 1; } }
        /// <summary>File extension typically used for this font type.</summary>
        public override String[] FileExtensions { get { return new String[] { "fnt" }; } }
        public override String ShortTypeName { get { return "CentFont"; } }
        public override String ShortTypeDescription { get { return "Centurion Font"; } }
        public override String LongTypeDescription { get { return "An 8 pixel wide 1-bit font that supports custom character widths and Y-offsets, but lacks a real header. It does not contain symbols below index 32, and adds an extra padding pixel behind all symbols."; } }
        public override String[] GamesListForType { get { return new String[] { "Centurion: Defender of Rome" }; } }
        
        protected const Int32 StartSymbol = 0x20;
        protected const Int32 NrOfSymbols = 0x60;

        public override void LoadFont(Byte[] fileData)
        {
            if (fileData.Length < 0xC0)
                throw new FileTypeLoadException(ERR_NOHEADER);
            if (fileData.Length % 0x60 != 0)
                throw new FileTypeLoadException(ERR_SIZECHECK);
            this.m_FontWidth = 8;
            this.m_FontHeight = (fileData.Length - 0xC0) / 0x60;

            Byte[] characterWidths = new Byte[NrOfSymbols];
            Array.Copy(fileData, 0, characterWidths, 0, NrOfSymbols);
            if (characterWidths.Any(w => w > 8))
                throw new FileTypeLoadException("Character widths data exceeds maximum width.");
            Byte[] characterYOffsets = new Byte[NrOfSymbols];
            Array.Copy(fileData, NrOfSymbols, characterYOffsets, 0, NrOfSymbols);
            Byte[][] characterData = new Byte[NrOfSymbols][];
            for (Int32 i = 0; i < NrOfSymbols; ++i)
            {
                Byte[] charData = new Byte[this.m_FontHeight];
                Array.Copy(fileData, NrOfSymbols * 2 + i * this.m_FontHeight, charData, 0, this.m_FontHeight);
                characterData[i] = charData;
            }
            for (Int32 i = 0; i < StartSymbol; ++i)
                this.m_ImageDataList.Add(new FontFileSymbol(new Byte[0], 0, this.m_FontHeight, 0, this.BitsPerPixel, this.TransparencyColor));
            for (Int32 i = 0; i < NrOfSymbols; ++i)
            {
                Byte[] curData8bit;
                try
                {
                    curData8bit = ImageUtils.ConvertTo8Bit(characterData[i], characterWidths[i], this.m_FontHeight,0, this.BitsPerPixel, true);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new FileTypeLoadException(String.Format("{0}: Data for font entry #{1} exceeds file bounds!", this.ShortTypeName, i));
                }
                FontFileSymbol fc = new FontFileSymbol(curData8bit, characterWidths[i], this.m_FontHeight, characterYOffsets[i], this.BitsPerPixel, this.TransparencyColor);
                this.m_ImageDataList.Add(fc);
            }
        }

        public override Byte[] SaveFont(SaveOption[] saveOptions)
        {
            Byte[] characterWidths = new Byte[NrOfSymbols];
            Byte[] characterYOffsets = new Byte[NrOfSymbols];
            Byte[][] imageData = new Byte[NrOfSymbols][];
            for (Int32 i = 0; i < NrOfSymbols; ++i)
            {
                FontFileSymbol ffs = this.m_ImageDataList[i+StartSymbol];
                imageData[i] = ImageUtils.ConvertFrom8Bit(ffs.ByteData, ffs.Width, this.m_FontHeight, this.BitsPerPixel, true);
                if (imageData[i].Length == 0)
                    imageData[i] = new Byte[this.m_FontHeight];
                characterWidths[i] = (Byte)ffs.Width;
                characterYOffsets[i] = (Byte)ffs.YOffset;
            }
            Byte[] fullData = new Byte[NrOfSymbols * (2 + this.m_FontHeight)];
            Int32 fontDataOffset = 0;
            Array.Copy(characterWidths, 0, fullData, fontDataOffset, NrOfSymbols);
            fontDataOffset += NrOfSymbols;
            Array.Copy(characterYOffsets, 0, fullData, fontDataOffset, NrOfSymbols);
            fontDataOffset += NrOfSymbols;
            for (Int32 i = 0; i < NrOfSymbols; ++i)
            {
                Array.Copy(imageData[i], 0, fullData, fontDataOffset, this.m_FontHeight);
                fontDataOffset += this.m_FontHeight;
            }
            return fullData;
        }
    }
}