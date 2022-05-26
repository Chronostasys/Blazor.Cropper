using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Blazor.Cropper
{
    /// <summary>
    /// Crop metadata
    /// </summary>
    public class CropInfo
    {
        /// <summary>
        /// Crop zone
        /// </summary>
        public Rectangle Rectangle { get; init; }
        public double Ratio { get; init; }

        /// <summary>
        /// Get init parameters from this function to restore the cropper state
        /// </summary>
        /// <returns></returns>
        public (int offsetX,int offsetY, int initX,int initY,double ratio) GetInitParams()
        {
            return ((int)(Rectangle.X ), (int)(Rectangle.Y ), (int)(Rectangle.Width), (int)(Rectangle.Height),Ratio);
        } 
    }
}
