using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.Cropper;

/// <summary>
///     js interop methods
/// </summary>
public static class JsInterop
{
    /// <summary>
    ///     set html img element to display the image
    ///     this function is the preferred way to display the crop result in dotnet 6
    /// </summary>
    /// <param name="js"></param>
    /// <param name="bin">image bytes</param>
    /// <param name="imgId">img element id</param>
    /// <param name="format">like image/jpg</param>
    /// <returns></returns>
    public static ValueTask SetImageAsync(this IJSRuntime js, byte[] bin, string imgId, string format)
        => js.InvokeVoidAsync("setSrc", bin, imgId, format);

    public static ValueTask LogAsync(this IJSRuntime js, params object[] objs)
        => js.InvokeVoidAsync("console.log", objs);
}