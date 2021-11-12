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
        private byte[] _data;
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

        public ImageCroppedResult(byte[] bytes)
        {
            _data = bytes;
        }
#if !NET6_0_OR_GREATER
        [Obsolete("this api is obsolete in dotnet 6 for bad performance")]
        public async Task<string> GetBase64Async()
        {
            if (_base64!=null)
            {
                return _base64;
            }
            return Convert.ToBase64String(await GetDataAsync());
        }
#endif
        /// <summary>
        /// get image bytes
        /// </summary>
        /// <returns>image bytes</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<byte[]> GetDataAsync()
        {
            if (_data!=null)
            {
                return _data;
            }
            if (_base64!=null)
            {
                _data = Convert.FromBase64String(_base64);
                return _data;
            }
            if (Img!=null)
            {
                using MemoryStream memoryStream = new MemoryStream();
                await Img.SaveAsync(memoryStream, Format);

                _data = memoryStream.ToArray();
                return _data;
            }
            throw new InvalidOperationException();
        }
        /// <summary>
        /// save img data to stream
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Task SaveAsync(Stream s)
        {
            if (Img != null)
            {
                return Img.SaveAsync(s, Format);
            }
            if (_data!=null)
            {
                return s.WriteAsync(_data).AsTask();
            }
            if (_base64!=null)
            {
                _data = Convert.FromBase64String(_base64);
                return s.WriteAsync(_data).AsTask();
            }
            throw new InvalidOperationException();
        }

        public void Dispose()
        {
            Img?.Dispose();
        }
    }
}