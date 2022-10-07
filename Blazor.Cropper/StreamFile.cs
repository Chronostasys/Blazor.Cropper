using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>
///     mock a browserfile from a stream
/// </summary>
public class StreamFile : IBrowserFile
{
    private readonly Stream _stream;

    /// <summary>
    ///     Build a stream file
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="name"></param>
    /// <param name="contentType"></param>
    public StreamFile(Stream stream, string name = "1.jpg", string contentType = "image/jpg")
    {
        _stream = stream;
        Name = name;
        ContentType = contentType;
    }

    /// <summary>
    ///     name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     not impl
    /// </summary>
    public DateTimeOffset LastModified => throw new NotImplementedException();

    /// <summary>
    ///     stream size
    /// </summary>
    public long Size => _stream.Length;

    /// <summary>
    ///     content type, should in form of image/xxx
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    ///     open read stream
    /// </summary>
    /// <param name="maxAllowedSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => _stream;
}