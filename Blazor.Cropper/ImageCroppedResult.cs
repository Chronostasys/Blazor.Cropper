using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Blazor.Cropper
{
    public class ImageCroppedResult:IDisposable
    {
        private string _base64;
        public Image Img { get; init; }
        public IImageFormat Format { get; init; }
        public ImageCroppedResult(Image img, IImageFormat format)
        {
            Img = img;
            Format = format;
        }
        public ImageCroppedResult(string base64)
        {
            _base64 = base64;
        }

        public async Task<string> GetBase64Async()
        {
            if (Img==null)
            {
                return _base64;
            }
            using var stream = new MemoryStream();
            await Img.SaveAsync(stream, Format);
            stream.TryGetBuffer(out ArraySegment<byte> buffer);
            return $"data:{Format.DefaultMimeType};base64,{Convert.ToBase64String(buffer.Array, 0, (int)stream.Length)}";
        }
        public Task SaveAsync(Stream s)
        {
            if (Img == null)
            {
                throw new NotSupportedException();
            }
            return Img.SaveAsync(s, Format);
        }

        public void Dispose()
        {
            Img?.Dispose();
        }
    }
}