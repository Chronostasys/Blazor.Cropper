using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;


public class ImageCroppedResult : IDisposable
{
    private readonly string _base64;
    private byte[] _data;

    public ImageCroppedResult(Image img, IImageFormat format, int quality)
    {
        Img = img;
        Format = format;
        Quality = quality;
    }

    public ImageCroppedResult(string base64, IImageFormat format)
    {
        _base64 = base64;
        Format = format;
    }

    public ImageCroppedResult(byte[] bytes, IImageFormat format)
    {
        _data = bytes;
        Format = format;
    }

    public Image Img { get; init; }
    public IImageFormat Format { get; init; }
    public int Quality { get; init; }

    public void Dispose()
    {
        Img?.Dispose();
    }
#if NET6_0_OR_GREATER
    [Obsolete("this api is obsolete in dotnet 6 for bad performance")]
#endif
    public async Task<string> GetBase64Async()
    {
        return _base64 ?? Convert.ToBase64String(await GetDataAsync());
    }

    /// <summary>
    ///     get image bytes
    /// </summary>
    /// <returns>image bytes</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<byte[]> GetDataAsync()
    {
        if (_data != null)
        {
            return _data;
        }

        if (_base64 != null)
        {
            _data = Convert.FromBase64String(_base64);
            return _data;
        }

        if (Img != null)
        {
            using MemoryStream memoryStream = new MemoryStream();
            IImageEncoder encoder = GetImageEncoder(Format, Quality);
            await Img.SaveAsync(memoryStream, encoder);

            _data = memoryStream.ToArray();
            return _data;
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    ///     save img data to stream
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public Task SaveAsync(Stream s)
    {
        if (Img != null)
        {
            IImageEncoder encoder = GetImageEncoder(Format, Quality);
            return Img.SaveAsync(s, encoder);
        }

        if (_data != null)
        {
            return s.WriteAsync(_data).AsTask();
        }

        if (_base64 == null)
        {
            throw new InvalidOperationException();
        }

        _data = Convert.FromBase64String(_base64);
        return s.WriteAsync(_data).AsTask();
    }

    private IImageEncoder GetImageEncoder(IImageFormat format, int quality) //TODO this variables are never used
    {
        IImageEncoder encoder = Configuration.Default.ImageFormatsManager.FindEncoder(Format);
        switch (encoder)
        {
            case JpegEncoder e:
                e.Quality = Quality;
                break;
            case WebpEncoder e:
                e.Quality = Quality;
                break;
            default:
                break;
        }

        return encoder;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    ///     Save image to a stream with your own
    ///     <see cref="IImageEncoder" />.
    ///     This can be use to
    ///     set some advanced options e.g. <see cref="PngCompressionLevel" /> for png.
    ///     This method can only be used when the cropper working in pure cs mode
    /// </summary>
    /// <param name="s"></param>
    /// <param name="encoder"></param>
    /// <returns></returns>
    public Task SaveAsync(Stream s, IImageEncoder encoder)
    {
        if (Img == null) throw new InvalidOperationException("This method can only be used when the cropper working in pure cs mode");
        return Img.SaveAsync(s, encoder);
    }
#endif
}