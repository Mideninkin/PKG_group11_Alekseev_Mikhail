using System;

public class ImageMetadata
{
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = "Unknown";
    public string Size { get; set; } = "N/A";
    public string Resolution { get; set; } = "N/A";
    public int BitDepth { get; set; }
    public string Compression { get; set; } = "N/A";
    public string Status { get; set; } = "OK";
}
