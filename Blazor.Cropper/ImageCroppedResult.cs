using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Blazor.Cropper
{
    public class ImageCroppedResult : IDisposable
    {
        private readonly string _base64;
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
            if (Img == null)
            {
                if (_base64.StartsWith("data:image/png"))
                {
                    string pureBase64 = _base64.Substring(22);
                    return pureBase64;
                }

                return _base64;
            }

            using MemoryStream memoryStream = new MemoryStream();
            await Img.SaveAsync(memoryStream, Format);

            byte[] imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
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