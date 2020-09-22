using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp;
namespace Blazor.Cropper
{
    public class ImageCroppedEventArgs
    {
        public IImageFormat Format{get;set;}
        public Image Image{get;set;}
        public double X{get;set;}
        public double Y{get;set;}
        public double Width{get;set;}
        public double Height{get;set;}
    }
}