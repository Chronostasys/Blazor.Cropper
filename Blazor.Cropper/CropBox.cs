using System;

namespace Blazor.Cropper
{
    class CropBox
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double H { get; private set; }
        public double W { get; private set; }

        private double _initX => _cropper._prevPosX;
        private double _initY => _cropper._prevPosY;
        private double _initW => _cropper.initCropWidth;
        private double _initH => _cropper.initCropHeight;
        private double _ratio => _cropper.AspectRatio;
        private double _imgh => _cropper._imgh;
        private double _imgw => _cropper._imgw;
        private double _unsavedX => _cropper._unsavedX;
        private double _unsavedY => _cropper._unsavedY;
        private double _unsavedW => _cropper._unsavedCropW;
        private double _unsavedH => _cropper._unsavedCropH;
        private double _minval => _cropper._minval;
        private bool _requestRatio => _cropper.RequireAspectRatio;
        private Cropper _cropper;

        private double _minposX => _cropper._minposX;
        private double _minposY => _cropper._minposY;

        internal CropBox(Cropper cropper)
        {
            X = cropper._prevPosX;
            Y = cropper._prevPosY;
            H = cropper.initCropHeight;
            W = cropper.initCropWidth;
            _cropper = cropper;
        }
        void xxLeft(double deltaX, double deltaY, MoveDir dir)
        {
            H = _initH +
                ((dir == MoveDir.UpLeft) ? (-deltaY) : deltaY);
            W = _initW - deltaX;
            if (_requestRatio)
            {
                W = H / _ratio;
                deltaX = _initW - W;
            }
            X = _initX + deltaX;
        }
        internal void DragCorner(double deltaX, double deltaY, MoveDir dir)
        {

            switch (dir)
            {
                case MoveDir.Up:
                    Y = _initY + deltaY;
                    H = _initH - deltaY;
                    break;
                case MoveDir.Down:
                    H = _initH + deltaY;
                    break;
                case MoveDir.Left:
                    X = _initX + deltaX;
                    W = _initW - deltaX;
                    break;
                case MoveDir.Right:
                    W = _initW + deltaX;
                    break;
                case MoveDir.UpLeft:
                    Y = _initY + deltaY;
                    xxLeft(deltaX, deltaY, dir);
                    break;
                case MoveDir.UpRight:
                    Y = _initY + deltaY;
                    H = _initH - deltaY;
                    W = _initW + deltaX;
                    break;
                case MoveDir.DownLeft:
                    xxLeft(deltaX, deltaY, dir);
                    break;
                case MoveDir.DownRight:
                    H = _initH + deltaY;
                    W = _initW + deltaX;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
            AdjustWH(dir);
            CheckOutOfBox();
            CheckMinWH();
        }
        internal void AdjustWH(MoveDir dir)
        {
            if (_requestRatio)
            {
                _ = dir switch
                {
                    MoveDir.Left or MoveDir.Right =>
                        H = W * _ratio,
                    MoveDir.UpLeft or MoveDir.DownLeft => 0,
                    MoveDir.UnKnown => throw new NotImplementedException(),
                    _ => W = H / _ratio
                };
            }
        }
        internal void CheckOutOfBox()
        {
            if (Y < _minposY)
            {
                var ydelta = Y - _minposY;
                Y = _minposY;
                H += ydelta;
            }
            if (X < _minposX)
            {
                var xdelta = X - _minposX;
                X = _minposX;
                W += xdelta;
            }
            if (Y + H > (_minposY + _imgh))
            {
                H = (_minposY + _imgh) - Y;
                if (_requestRatio)
                {
                    W = H / _ratio;
                }
            }
            if (X + W > (_minposX + _imgw))
            {
                W = (_minposX + _imgw) - X;
                if (_requestRatio)
                {
                    H = W / _ratio;
                }
            }
        }
        internal void CheckMinWH()
        {
            if (H < _minval)
            {
                H = _unsavedH;
                Y = _unsavedY;
            }
            if (W < _minval)
            {
                W = _unsavedW;
                X = _unsavedX;
            }
        }
    }
}