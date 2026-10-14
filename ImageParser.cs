using System;
using System.IO;
using System.Text;

public static class ImageParser
{
    public static ImageMetadata ParseFile(string filePath)
    {
        var meta = new ImageMetadata { FileName = Path.GetFileName(filePath) };
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4)
            {
                meta.Status = "Файл поврежден (слишком мал)";
                return meta;
            }

            byte[] signature = new byte[4];
            fs.ReadExactly(signature, 0, 4);
            fs.Position = 0;

            if (signature[0] == 0x42 && signature[1] == 0x4D) return ParseBmp(fs, meta);
            if (signature[0] == 0x89 && signature[1] == 0x50 && signature[2] == 0x4E && signature[3] == 0x47) return ParsePng(fs, meta);
            if (signature[0] == 0xFF && signature[1] == 0xD8) return ParseJpg(fs, meta);
            if (signature[0] == 0x47 && signature[1] == 0x49 && signature[2] == 0x46) return ParseGif(fs, meta);
            if ((signature[0] == 0x49 && signature[1] == 0x49) || (signature[0] == 0x4D && signature[1] == 0x4D)) return ParseTiff(fs, meta);
            if (signature[0] == 0x0A) return ParsePcx(fs, meta);

            meta.Status = "Неподдерживаемый формат / Подмена расширения";
        }
        catch (Exception ex)
        {
            meta.Status = $"Ошибка: {ex.Message}";
        }
        return meta;
    }

    private static ImageMetadata ParseBmp(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "BMP";
        byte[] header = new byte[54];
        if (fs.Read(header, 0, 54) < 54) { meta.Status = "Файл поврежден"; return meta; }

        int width = BitConverter.ToInt32(header, 18);
        int height = BitConverter.ToInt32(header, 22);
        meta.Size = $"{Math.Abs(width)}x{Math.Abs(height)}";
        meta.BitDepth = BitConverter.ToInt16(header, 28);
        
        int rX = BitConverter.ToInt32(header, 38);
        int rY = BitConverter.ToInt32(header, 42);
        meta.Resolution = rX > 0 ? $"{Math.Round(rX * 0.0254)}x{Math.Round(rY * 0.0254)} DPI" : "96x96 DPI";

        int comp = BitConverter.ToInt32(header, 30);
        meta.Compression = comp == 1 ? "BI_RLE8" : comp == 2 ? "BI_RLE4" : "BI_RGB (Нет)";
        return meta;
    }

    private static ImageMetadata ParsePng(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "PNG";
        byte[] header = new byte[33]; 
        if (fs.Read(header, 0, 33) < 33) { meta.Status = "Файл поврежден"; return meta; }

        int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        meta.Size = $"{width}x{height}";
        meta.BitDepth = header[24];
        meta.Compression = "Deflate";

        if (fs.Length > 12)
        {
            fs.Position = fs.Length - 12;
            byte[] endBytes = new byte[12];
            fs.ReadExactly(endBytes, 0, 12);
            string endStr = Encoding.ASCII.GetString(endBytes, 4, 4);
            if (endStr != "IEND") meta.Status = "Файл поврежден (Нет IEND)";
        }
        return meta;
    }

    private static ImageMetadata ParseJpg(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "JPEG";
        meta.Compression = "JPEG";
        

        if (fs.Length > 2)
        {
            fs.Position = fs.Length - 2;
            if (fs.ReadByte() != 0xFF || fs.ReadByte() != 0xD9) meta.Status = "Файл поврежден (Нет EOI)";
        }

        fs.Position = 2;
        while (fs.Position < fs.Length)
        {
            int markerMarker = fs.ReadByte();
            int marker = fs.ReadByte();
            if (markerMarker != 0xFF) break;

            if (marker == 0xD9 || marker == 0xDA) break;

            int length = (fs.ReadByte() << 8) | fs.ReadByte();
            long nextPosition = fs.Position + length - 2;

            if (marker == 0xE0)
            {
                byte[] jfif = new byte[7];
                fs.ReadExactly(jfif, 0, 7);
                int units = jfif[2];
                int densityX = (jfif[3] << 8) | jfif[4];
                int densityY = (jfif[5] << 8) | jfif[6];
                meta.Resolution = units == 1 ? $"{densityX}x{densityY} DPI" : "72x72 DPI";
            }
            else if (marker == 0xC0 || marker == 0xC2)
            {
                meta.BitDepth = fs.ReadByte() * 3;
                int height = (fs.ReadByte() << 8) | fs.ReadByte();
                int width = (fs.ReadByte() << 8) | fs.ReadByte();
                meta.Size = $"{width}x{height}";
            }
            fs.Position = nextPosition;
        }
        if (meta.Resolution == "N/A") meta.Resolution = "72x72 DPI";
        return meta;
    }

    private static ImageMetadata ParseGif(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "GIF";
        byte[] header = new byte[13];
        if (fs.Read(header, 0, 13) < 13) { meta.Status = "Файл поврежден"; return meta; }

        int width = BitConverter.ToUInt16(header, 6);
        int height = BitConverter.ToUInt16(header, 8);
        meta.Size = $"{width}x{height}";
        meta.BitDepth = ((header[10] & 0x07) + 1);
        meta.Compression = "LZW";
        meta.Resolution = "96x96 DPI";
        return meta;
    }

    private static ImageMetadata ParseTiff(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "TIFF";
        byte[] header = new byte[8];
        fs.ReadExactly(header, 0, 8);
        bool isLittleEndian = header[0] == 0x49;

        Func<byte[], int, int> readInt = (b, offset) => isLittleEndian ? BitConverter.ToInt32(b, offset) : (b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3];
        Func<byte[], int, int> readShort = (b, offset) => isLittleEndian ? BitConverter.ToUInt16(b, offset) : (b[offset] << 8) | b[offset + 1];

        int ifdOffset = readInt(header, 4);
        fs.Position = ifdOffset;

        byte[] countBytes = new byte[2];
        fs.ReadExactly(countBytes, 0, 2);
        int entryCount = readShort(countBytes, 0);

        for (int i = 0; i < entryCount; i++)
        {
            byte[] entry = new byte[12];
            fs.ReadExactly(entry, 0, 12);
            int tag = readShort(entry, 0);
            int type = readShort(entry, 2);
            int val = type == 3 ? readShort(entry, 8) : readInt(entry, 8);

            if (tag == 256) meta.Size = $"{val}x" + meta.Size.Split('x').LastOrDefault();
            if (tag == 257) meta.Size = meta.Size.Split('x').FirstOrDefault() + $"x{val}";
            if (tag == 258) meta.BitDepth = val;
            if (tag == 259) meta.Compression = val == 1 ? "None" : val == 5 ? "LZW" : val == 6 ? "JPEG" : $"Type {val}";
        }
        if (meta.Resolution == "N/A") meta.Resolution = "300x300 DPI";
        return meta;
    }

    private static ImageMetadata ParsePcx(FileStream fs, ImageMetadata meta)
    {
        meta.Format = "PCX";
        byte[] header = new byte[128];
        if (fs.Read(header, 0, 128) < 128) { meta.Status = "Файл поврежден"; return meta; }

        int xMin = BitConverter.ToInt16(header, 4);
        int yMin = BitConverter.ToInt16(header, 6);
        int xMax = BitConverter.ToInt16(header, 8);
        int yMax = BitConverter.ToInt16(header, 10);
        
        meta.Size = $"{xMax - xMin + 1}x{yMax - yMin + 1}";
        meta.BitDepth = header[3] * header[65];
        meta.Compression = header[2] == 1 ? "RLE" : "None";
        meta.Resolution = $"{BitConverter.ToInt16(header, 12)}x{BitConverter.ToInt16(header, 14)} DPI";
        return meta;
    }
}
