using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp;
using System;
namespace Blazor.Cropper
{
    public class ImageCroppedResult
    {
        public string Base64{get;set;}
        public byte[] GetBytes()
        {
            return Convert.FromBase64String(Base64.Substring(Base64.IndexOf(',')+1));
        }
        public double X{get;set;}
        public double Y{get;set;}
        public double Width{get;set;}
        public double Height{get;set;}
    }
}