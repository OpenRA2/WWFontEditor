﻿using System;
using System.Linq;
using Nyerguds.Util;
using Nyerguds.ImageManipulation;

namespace WWFontEditor.Domain.FontTypes
{
    public class FontFileKort : FontFile
    {
        public override Int32 SymbolsTypeMin { get { return 0x80; } }
        public override Int32 SymbolsTypeMax { get { return 0x80; } }
        public override Int32 SymbolsTypeFirst { get { return 0x20; } }
        public override Int32 FontHeightTypeMax { get { return 0xFF; } }
        public override Int32 FontWidthTypeMax { get { return 0x100; } }
        public override Int32 YOffsetTypeMax { get { return 0xFF; } }
        /// <summary> Set this to False if individual symbols cannot have a different width than their parent font.</summary>
        public override Boolean CustomSymbolWidthsForType { get { return true; } }
        /// <summary> Set this to False if individual symbols cannot have a different height than their parent font.</summary>
        public override Boolean CustomSymbolHeightsForType { get { return false; } }
        public override Int32 BitsPerPixel { get { return 1; } }
        /// <summary>File extension typically used for this font type.</summary>
        public override String[] FileExtensions { get { return new String[] { "fnt" }; } }
        public override String ShortTypeName { get { return "KORTFont"; } }
        public override String ShortTypeDescription { get { return "King Arthur's KORT Font"; } }
        public override String LongTypeDescription { get { return "A 1-bit font which defines overall width and height in bytes, which contains individual symbol sizes, and Y-offsets saved from the bottom. It does not contain symbols below index 32, or above 127."; } }
        public override String[] GamesListForType { get { return new String[] { "King Arthur's Knights of the Round Table" }; } }
        

        public override void LoadFont(Byte[] fileData)
        {
            if (fileData.Length < 0xC2)
                throw new FileTypeLoadException(ERR_NOHEADER);
            if ((fileData.Length - 2) % 0x60 != 0)
                throw new FileTypeLoadException(ERR_SIZECHECK);

            Int32 fontWidthBytes = fileData[0];
            this.m_FontWidth = fontWidthBytes * 8;
            this.m_FontHeight = fileData[1];
            Int32 nrOfSymbols = this.SymbolsTypeMax - this.SymbolsTypeFirst;
            Byte[] symbolWidths = new Byte[nrOfSymbols];
            Array.Copy(fileData, 2, symbolWidths, 0, nrOfSymbols);
            if (symbolWidths.Any(w => w > this.m_FontWidth))
                throw new FileTypeLoadException("Character widths data exceeds maximum width.");
            Byte[] symbolYOffsets = new Byte[nrOfSymbols];
            Array.Copy(fileData, nrOfSymbols + 2, symbolYOffsets, 0, nrOfSymbols);
            if (symbolYOffsets.Any(w => w > this.m_FontHeight))
                throw new FileTypeLoadException("Character delta-Y exceeds maximum height.");

            Byte[][] symbolData = new Byte[nrOfSymbols][];
            Int32 charSize = this.m_FontHeight * fontWidthBytes;
            for (Int32 i = 0; i < nrOfSymbols; ++i)
            {
                Byte[] charData = new Byte[charSize];
                Array.Copy(fileData, nrOfSymbols * 2 + 2 + i * charSize, charData, 0, charSize);
                symbolData[i] = charData;
            }
            for (Int32 i = 0; i < this.SymbolsTypeFirst; ++i)
                this.m_ImageDataList.Add(new FontFileSymbol(new Byte[0], 0, this.m_FontHeight, 0, this.BitsPerPixel, this.TransparencyColor));
            for (Int32 i = 0; i < nrOfSymbols; ++i)
            {
                Byte[] curData8bit;
                try
                {
                    curData8bit = ImageUtils.ConvertTo8Bit(symbolData[i], symbolWidths[i], this.m_FontHeight, 0, this.BitsPerPixel, true);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new IndexOutOfRangeException(String.Format("Data for font entry #{0} exceeds file bounds!", i));
                }
                FontFileSymbol fc = new FontFileSymbol(curData8bit, symbolWidths[i], this.m_FontHeight, this.m_FontHeight - symbolYOffsets[i], this.BitsPerPixel, this.TransparencyColor);
                this.m_ImageDataList.Add(fc);
            }
        }

        public override Byte[] SaveFont(SaveOption[] saveOptions)
        {
            Int32 fontStride = (this.m_FontWidth + 7) / 8;
            Int32 charSize = this.m_FontHeight * fontStride;
            Int32 nrOfSymbols = this.SymbolsTypeMax - this.SymbolsTypeFirst;
            Byte[] characterWidths = new Byte[nrOfSymbols];
            Byte[] characterYOffsets = new Byte[nrOfSymbols];
            Byte[][] imageData = new Byte[nrOfSymbols][];
            Int32 curSymbol = this.SymbolsTypeFirst;
            for (Int32 i = 0; i < nrOfSymbols; ++i)
            {
                FontFileSymbol ffs = this.m_ImageDataList[curSymbol++];
                Byte[] ffsBytes = ImageUtils.ConvertFrom8Bit(ffs.ByteData, ffs.Width, this.m_FontHeight, this.BitsPerPixel, true);
                if (ffsBytes.Length < charSize)
                {
                    Byte[] symbolBytes = new Byte[charSize];
                    Array.Copy(ffsBytes, 0, symbolBytes, 0, ffsBytes.Length);
                    ffsBytes = symbolBytes;
                }
                imageData[i] = ffsBytes;
                characterWidths[i] = (Byte)ffs.Width;
                characterYOffsets[i] = (Byte)Math.Min(Math.Max(0, this.m_FontHeight - ffs.YOffset), this.m_FontHeight);
            }
            Byte[] fullData = new Byte[nrOfSymbols * (2 + (this.m_FontHeight * fontStride)) + 2];
            fullData[0x00] = (Byte)fontStride;
            fullData[0x01] = (Byte)this.m_FontHeight;
            Int32 fontDataOffset = 2;
            Array.Copy(characterWidths, 0, fullData, fontDataOffset, nrOfSymbols);
            fontDataOffset += nrOfSymbols;
            Array.Copy(characterYOffsets, 0, fullData, fontDataOffset, nrOfSymbols);
            fontDataOffset += nrOfSymbols;
            for (Int32 i = 0; i < nrOfSymbols; ++i)
            {
                Array.Copy(imageData[i], 0, fullData, fontDataOffset, charSize);
                fontDataOffset += charSize;
            }
            return fullData;
        }
    }
}