using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AccessibleOCRStudio.Services;

/// <summary>Image-preserving searchable PDF output shared by both OCR engines.</summary>
public static class SearchablePdfWriter
{
    private static string Number(double value)=>value.ToString("0.######",CultureInfo.InvariantCulture);
    public static void Write(string image,string output,OcrPageResult recognized)
    {
        BitmapSource bitmap;
        using(var source=File.OpenRead(image))
            bitmap=BitmapDecoder.Create(source,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
        if(recognized.Angle is 90 or 180 or 270)
            bitmap=new TransformedBitmap(bitmap,new RotateTransform(-recognized.Angle));
        bitmap=new FormatConvertedBitmap(bitmap,PixelFormats.Rgb24,null,0);
        var width=bitmap.PixelWidth;var height=bitmap.PixelHeight;
        var pixels=new byte[checked(width*height*3)];bitmap.CopyPixels(pixels,width*3,0);
        byte[] compressed;
        using(var buffer=new MemoryStream())
        {
            using(var zip=new ZLibStream(buffer,CompressionLevel.Optimal,true))zip.Write(pixels);
            compressed=buffer.ToArray();
        }
        const double scale=72d/300;
        var content=new StringBuilder(FormattableString.Invariant($"q {Number(width*scale)} 0 0 {Number(height*scale)} 0 0 cm /Im0 Do Q\n"));
        foreach(var line in recognized.Lines)
        {
            if(string.IsNullOrEmpty(line.Text))continue;
            var rtl=line.Text.FirstOrDefault(char.IsLetter) is >= '\u0600' and <= '\u06ff';
            var formatted=new FormattedText(line.Text,CultureInfo.GetCultureInfo(rtl?"ar":"fr"),rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight,new Typeface("Arial"),100,Brushes.Black,1);
            var whole=formatted.BuildHighlightGeometry(new Point(0,0),0,line.Text.Length)?.Bounds ?? Rect.Empty;
            if(whole.IsEmpty || whole.Width<=0)continue;
            var font=Math.Max(1,(line.Box[3]-line.Box[1])*scale*.8);
            // Use WPF's bidirectional character geometry, then map each original
            // Unicode character to one invisible glyph at its visual position.
            // This avoids making the PDF writer depend on optional Paddle/Python.
            for(var index=0;index<line.Text.Length;index++)
            {
                var length=char.IsHighSurrogate(line.Text[index]) && index+1<line.Text.Length && char.IsLowSurrogate(line.Text[index+1])?2:1;
                var character=line.Text.Substring(index,length);
                var bounds=formatted.BuildHighlightGeometry(new Point(0,0),index,length)?.Bounds ?? Rect.Empty;
                if(bounds.IsEmpty)continue;
                var x=(line.Box[0]+(bounds.Left-whole.Left)/whole.Width*(line.Box[2]-line.Box[0]))*scale;
                var y=(height-line.Box[3])*scale;
                var glyphWidth=Math.Max(.05,bounds.Width/whole.Width*(line.Box[2]-line.Box[0])*scale);
                var horizontalScale=glyphWidth/(font*.556)*100;
                var actual=Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes("\uFEFF"+character));
                content.Append($"/Span << /ActualText <{actual}> >> BDC\nBT /F0 {Number(font)} Tf 3 Tr {Number(horizontalScale)} Tz 1 0 0 1 {Number(x)} {Number(y)} Tm (?) Tj ET\nEMC\n");
                index+=length-1;
            }
        }
        byte[] StreamObject(string dictionary,byte[] bytes)
        {
            using var buffer=new MemoryStream();
            buffer.Write(Encoding.ASCII.GetBytes($"<< {dictionary} /Length {bytes.Length} >>\nstream\n"));
            buffer.Write(bytes);buffer.Write(Encoding.ASCII.GetBytes("\nendstream"));return buffer.ToArray();
        }
        byte[] Ascii(string text)=>Encoding.ASCII.GetBytes(text);
        var objects=new[]{
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(width*scale)} {Number(height*scale)}] /Resources << /Font << /F0 5 0 R >> /XObject << /Im0 4 0 R >> >> /Contents 6 0 R >>"),
            StreamObject($"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode",compressed),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"),
            StreamObject(string.Empty,Ascii(content.ToString()))
        };
        using var destination=File.Create(output);
        destination.Write(Ascii("%PDF-1.7\n"));
        var offsets=new List<long>{0};
        for(var index=0;index<objects.Length;index++)
        {
            offsets.Add(destination.Position);destination.Write(Ascii($"{index+1} 0 obj\n"));
            destination.Write(objects[index]);destination.Write(Ascii("\nendobj\n"));
        }
        var xref=destination.Position;
        destination.Write(Ascii($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n"));
        foreach(var offset in offsets.Skip(1))destination.Write(Ascii(offset.ToString("D10",CultureInfo.InvariantCulture)+" 00000 n \n"));
        destination.Write(Ascii($"trailer\n<< /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"));
    }
}
