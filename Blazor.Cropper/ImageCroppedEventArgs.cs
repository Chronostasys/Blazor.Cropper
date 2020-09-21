using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp;
namespace Blazor.Cropper
{
    public class ImageCroppedEventArgs
    {
        public IImageFormat Format{get;set;}
        public Image Image{get;set;}
    }
}