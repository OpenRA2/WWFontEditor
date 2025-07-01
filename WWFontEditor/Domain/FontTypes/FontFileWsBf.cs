﻿using System;
using System.Text;
using Nyerguds.Util;
using Nyerguds.ImageManipulation;

namespace WWFontEditor.Domain.FontTypes
{
    /// <summary>
    /// Westwood Studios RA2/Nox font format.
    /// </summary>
    public class FontFileWsBf : FontFile
    {
        public override Int32 SymbolsTypeMax { get { return 0x100; } }
        public override Int32 FontWidthTypeMax { get { return 0xFF; } }
        public override Int32 FontHeightTypeMax { get { return 0xFF; } }
        /// <summary> Set this to False if individual symbols cannot have different sizes than their parent font.</summary>
        public override Boolean CustomSymbolHeightsForType { get { return false; } }
        public override Int32 YOffsetTypeMax { get { return 0; } }
        /// <summary>Padding between the characters of the font. Used for the preview function and to determine if padding is needed when automatically optimizing symbol widths.</summary>
        public override Int32 FontTypePaddingHorizontal { get { return 1; } }
        public override Int32 BitsPerPixel { get { return 1; } }

        public override String ShortTypeName { get { return "WW BitFont (RA2)"; } }
        public override String ShortTypeDescription { get { return "WW BitFont (RA2)"; } }
        public override String LongTypeDescription { get { return "A 1-bpp font which saves only the used range."; } }
        public override String[] GamesListForType { get { return new String[] { "Command & Conquer Red Alert 2" }; } }

        public override void LoadFont(Byte[] fileData)
        {
            this.LoadFromFile(fileData, false);
        }

        public void LoadFromFile(Byte[] fileData, Boolean forNox)
        {
            if (fileData.Length < 0x30)
                throw new FileTypeLoadException(ERR_NOHEADER);
            String format = Encoding.ASCII.GetString(fileData, 0, 4);
            Boolean isRa2 = String.Equals(format, "FoNt", StringComparison.InvariantCulture);
            Boolean isNox = String.Equals(format, "tNoF", StringComparison.InvariantCulture);
            if ((isRa2 && forNox) || (isNox && !forNox))
                throw new FileTypeLoadException(ERR_BADHEADER);
            Int32 stride;
            Int32 symbolDataSize;
            Int32 startSymbol;
            Int32 endSymbol;
            Int32 readOffset;
            Int32 fontDataHeight;
            this.m_FontWidth = 0;
            if (isRa2)
            {
                // Might be ideograph / space width?
                //this.m_FontWidth = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
                stride = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 4, true);
                fontDataHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 4, true);
                this.m_FontHeight = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 4, true);
                // should always be "1". Not gonna support anything else until it's confirmed
                Int32 bppFormat = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x14, 4, true);
                if (bppFormat != 1)
                    throw new FileTypeLoadException(ERR_BADHEADER);
                symbolDataSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 4, true);
                //UInt32 dword1C = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0x1C, 4, true); // always 0x24
                //UInt32 dword20 = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0x20, 4, true); // always 0x30
                //UInt32 dword24 = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0x24, 4, true); // always 0x00
                startSymbol = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x28, 4, true);
                endSymbol = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x2C, 4, true);
                readOffset = 0x30;
            }
            else
            {
                //UInt32 dword04 = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true); // always 0x01
                // Might be ideograph / space width?
                //this.m_FontWidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 4, true);
                stride = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 4, true);
                fontDataHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 4, true);
                this.m_FontHeight = fontDataHeight;
                // should always be "1". Not gonna support anything else until it's confirmed
                Int32 bppFormat = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x14, 4, true);
                if (bppFormat != 1)
                    throw new FileTypeLoadException(ERR_BADHEADER);
                symbolDataSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 4, true);
                //UInt32 dword1C = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0x1C, 4, true); // always 0x00
                startSymbol = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x20, 2, true);
                endSymbol = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x22, 2, true);
                readOffset = 0x24;
            }
            Int32 symbolImageSize = stride * fontDataHeight;
            if (symbolDataSize != symbolImageSize + 1)
                throw new FileTypeLoadException(ERR_BADHEADER);
            for (Int32 i = 0; i < startSymbol; ++i)
                this.m_ImageDataList.Add(new FontFileSymbol(this));
            for (Int32 i = startSymbol; i <= endSymbol; ++i)
            {
                if (readOffset + symbolDataSize > fileData.Length)
                    throw new FileTypeLoadException("File is not long enough to contain all symbols!");
                Byte symbolWidth = fileData[readOffset++];
                // Technically the read font width is irrelevant, and thus it might be wrong.
                if (symbolWidth > this.m_FontWidth && ImageUtils.GetMinimumStride(symbolWidth, 1) <= stride)
                    this.m_FontWidth = symbolWidth;
                Byte[] symbolData = new Byte[symbolImageSize];
                Array.Copy(fileData, readOffset, symbolData, 0, symbolImageSize);
                Int32 symbolStride = stride;
                Byte[] symbolData8Bit = ImageUtils.ConvertTo8Bit(symbolData, symbolWidth, fontDataHeight, 0, 1, true, ref symbolStride);
                FontFileSymbol ffs = new FontFileSymbol(symbolData8Bit, symbolWidth, fontDataHeight, 0, 1, this.TransparencyColor);
                ffs.ChangeHeight(m_FontHeight);
                this.m_ImageDataList.Add(ffs);
                readOffset += symbolImageSize;
            }
        }

        public override Byte[] SaveFont(SaveOption[] saveOptions)
        {
            return this.SaveFont(false, saveOptions);
        }

        public Byte[] SaveFont(Boolean asNox, SaveOption[] saveOptions)
        {
            Int32 totalCount = this.m_ImageDataList.Count;
            Int32 startSymbol = 0;
            for (; startSymbol < totalCount; ++startSymbol)
                if (this.m_ImageDataList[startSymbol].Width > 0)
                    break;
            Int32 endSymbol = totalCount - 1;
            for (; endSymbol > startSymbol ; endSymbol--)
                if (this.m_ImageDataList[endSymbol].Width != 0)
                    break;
            Int32 count = endSymbol - startSymbol + 1;
            Int32 fontDataHeight = 0;
            Int32 fontDataWidth = 0;
            for (Int32 i = startSymbol; i < endSymbol; ++i)
            {
                FontFileSymbol ffs = m_ImageDataList[i];
                Int32 symbWidth = ffs.Width;
                if (symbWidth == 0)
                    continue;
                if (fontDataWidth < symbWidth)
                    fontDataWidth = symbWidth;
                Int32 yoffSet = 0;
                Int32 height = ffs.Height;
                ImageUtils.OptimizeYHeight(ffs.ByteData, ffs.Width, ref height, ref yoffSet, true, this.TransparencyColor, this.FontHeight, false);
                Int32 newHeight = height + yoffSet;
                if (newHeight > fontDataHeight)
                    fontDataHeight = newHeight;
            }
            if (asNox)
                fontDataHeight = m_FontHeight;
            Int32 stride = ImageUtils.GetMinimumStride(fontDataWidth, 1);
            Int32 symbolImageSize = stride * fontDataHeight;
            Int32 symbolDataSize = symbolImageSize + 1;
            Int32 writeOffset = (!asNox ? 0x30 : 0x24);
            Byte[] outputArray = new Byte[writeOffset + count * symbolDataSize];
            if (!asNox)
            {
                Array.Copy(Encoding.ASCII.GetBytes("FoNt"), outputArray, 4);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x04, 4, true, (UInt32)fontDataWidth);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x08, 4, true, (UInt32)stride);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x0C, 4, true, (UInt32)fontDataHeight);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x10, 4, true, (UInt32)this.m_FontHeight);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x14, 4, true, 1);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x18, 4, true, (UInt32)symbolDataSize);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x1C, 4, true, 0x24);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x20, 4, true, 0x30);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x24, 4, true, 0x00);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x28, 4, true, (UInt32)startSymbol);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x2C, 4, true, (UInt32)endSymbol);
            }
            else
            {
                Array.Copy(Encoding.ASCII.GetBytes("tNoF"), outputArray, 4);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x04, 4, true, 1);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x08, 4, true, (UInt32)fontDataWidth);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x0C, 4, true, (UInt32)stride);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x10, 4, true, (UInt32)this.m_FontHeight);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x14, 4, true, 1);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x18, 4, true, (UInt32)symbolDataSize);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x1C, 4, true, 0x00);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x20, 2, true, (UInt32)startSymbol);
                ArrayUtils.WriteIntToByteArray(outputArray, 0x22, 2, true, (UInt32)endSymbol);
            }
            for (Int32 i = startSymbol; i <= endSymbol; ++i)
            {
                FontFileSymbol ffs = this.m_ImageDataList[i];
                Int32 symbWidth = ffs.Width;
                if (fontDataHeight != m_FontHeight)
                {
                    ffs = ffs.Clone();
                    ffs.ChangeHeight(fontDataHeight);
                }
                outputArray[writeOffset++] = (Byte)symbWidth;
                Int32 symbStride = symbWidth;
                Byte[] oneBppArr = ImageUtils.ConvertFrom8Bit(ffs.ByteData, symbWidth, ffs.Height, 1, true, ref symbStride);
                oneBppArr = ImageUtils.ChangeStride(oneBppArr, symbStride, ffs.Height, stride, false, 0);
                Array.Copy(oneBppArr, 0, outputArray, writeOffset, symbolImageSize);
                writeOffset += symbolImageSize;
            }
            return outputArray;
        }
    }

    public class FontFileWsBfNox : FontFileWsBf
    {
        public override String ShortTypeName { get { return "WW BitFont (NoX)"; } }
        public override String ShortTypeDescription { get { return "WW BitFont (NoX)"; } }
        public override String[] GamesListForType { get { return new String[] { "NoX" }; } }
        
        public override void LoadFont(Byte[] fileData)
        {
            this.LoadFromFile(fileData, true);
        }
        public override Byte[] SaveFont(SaveOption[] saveOptions)
        {
            return this.SaveFont(true, saveOptions);
        }

    }
}
