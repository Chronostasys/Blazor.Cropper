using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Cropper
{
    /// <summary>
    /// js interop methods
    /// </summary>
    public static class JSInterop
    {
        /// <summary>
        /// set html img element to display the image
        /// this function is the preferred way to display the crop result in dotnet 6
        /// </summary>
        /// <param name="js"></param>
        /// <param name="bin">image bytes</param>
        /// <param name="imgid">img element id</param>
        /// <returns></returns>
        public static ValueTask SetImageAsync(this IJSRuntime js,byte[] bin,string imgid)
        {
            return js.InvokeVoidAsync("setSrc", bin, imgid);
        }
    }
}
