using System;
using System.Collections.Generic;
using Blazor.Cropper;


internal class CropBox
{
    private readonly Cropper _cropper;

    internal CropBox(Cropper cropper)
    {
        X = cropper._prevPosX;
        Y = cropper._prevPosY;
        H = cropper.initCropHeight;
        W = cropper.initCropWidth;
        _cropper = cropper;
    }

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

    private double _minposX => _cropper._minposX;
    private double _minposY => _cropper._minposY;
    private double _minvalx => _cropper._minvalx;
    private double _minvaly => _cropper._minvaly;

    private void xxLeft(double deltaX, double deltaY, MoveDir dir)
    {
        H = _initH +
            (dir == MoveDir.UpLeft ? -deltaY : deltaY);
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
        Dictionary<MoveDir, Action> funcdic = new Dictionary<MoveDir, Action>
        {
            {
                MoveDir.Up, () =>
                {
                    Y = _initY + deltaY;
                    H = _initH - deltaY;
                }
            },
            { MoveDir.Down, () => H = _initH + deltaY },
            {
                MoveDir.Left, () =>
                {
                    X = _initX + deltaX;
                    W = _initW - deltaX;
                }
            },
            { MoveDir.Right, () => W = _initW + deltaX },
            {
                MoveDir.UpLeft, () =>
                {
                    Y = _initY + deltaY;
                    xxLeft(deltaX, deltaY, dir);
                }
            },
            {
                MoveDir.UpRight, () =>
                {
                    Y = _initY + deltaY;
                    H = _initH - deltaY;
                    W = _initW + deltaX;
                }
            },
            { MoveDir.DownLeft, () => xxLeft(deltaX, deltaY, dir) },
            {
                MoveDir.DownRight, () =>
                {
                    H = _initH + deltaY;
                    W = _initW + deltaX;
                }
            }
        };
        funcdic[dir]();
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
                MoveDir.Left or MoveDir.Right => H = W * _ratio,
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
            double ydelta = Y - _minposY;
            Y = _minposY;
            H += ydelta;
            W = _requestRatio ? H / _ratio : W;
            if (X - _unsavedX < 0)
            {
                X = _unsavedX - (W - _unsavedW);
            }
        }

        if (X < _minposX)
        {
            double xdelta = X - _minposX;
            X = _minposX;
            W += xdelta;
            H = _requestRatio ? W * _ratio : H;
            if (Y - _unsavedY < 0)
            {
                Y = _unsavedY - (H - _unsavedH);
            }
        }

        if (Y + H > _minposY + _imgh)
        {
            H = _minposY + _imgh - Y;
            W = _requestRatio ? H / _ratio : W;
            if (Y - _unsavedY < 0) Y = _unsavedY - (H - _unsavedH);
            if (X - _unsavedX < 0) X = _unsavedX - (W - _unsavedW);
        }

        if (X + W > _minposX + _imgw)
        {
            W = _minposX + _imgw - X;
            H = _requestRatio ? W * _ratio : H;
            if (Y - _unsavedY < 0) Y = _unsavedY - (H - _unsavedH);
        }
    }

    internal void CheckMinWH()
    {
        if (H < _minvaly)
        {
            H = _unsavedH;
            Y = _unsavedY;
            W = _unsavedW;
            X = _unsavedX;
        }

        if (W < _minvalx)
        {
            W = _unsavedW;
            X = _unsavedX;
            H = _unsavedH;
            Y = _unsavedY;
        }
    }
}