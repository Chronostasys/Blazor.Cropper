using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Blazor.Cropper
{
    public partial class Cropper:IAsyncDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        #region params

        /// <summary>
        /// whether use c# for ordinary img processing
        /// </summary>
        /// <value>default: false</value>
        [Parameter]
        public bool PureCSharpProcessing { get; set; } = false;
        /// <summary>
        /// If this property is true, the image will become
        /// unresizable in cropper
        /// </summary>
        /// <value>default: true</value>
        [Parameter]
        public bool IsImageLocked { get; set; } = true;
        /// <summary>
        /// the initial width of cropper if possible.
        /// shall not be smaller than 30
        /// </summary>
        /// <value>default: 150</value>
        [Parameter]
        public double InitCropWidth { get; set; } = 150;

        private double initCropWidth = -1;
        /// <summary>
        /// the initial height of cropper if possible.
        /// shall not be smaller than 30
        /// </summary>
        /// <value>default: 150</value>
        [Parameter]
        public double InitCropHeight { get; set; } = 150;

        private double initCropHeight = -1;
        /// <summary>
        /// sepecify whether the cropper's aspect ratio is fixed
        /// </summary>
        /// <value>default: false</value>
        [Parameter]
        public bool RequireAspectRatio { get; set; } = false;
        /// <summary>
        /// sepecify the cropper's aspect ratio
        /// </summary>
        /// <remarks>Only works when <see cref="RequireAspectRatio"/>
        /// is true</remarks>
        /// <value>default: 1</value>
        [Parameter]
        public double AspectRatio { get; set; } = 1;
        /// <summary>
        /// The input image file. Usually get from an
        /// <see cref="InputFile"/> Component. You can also
        /// mock a file from stream using <see cref="StreamFile"/> if needed.
        /// </summary>
        /// <value></value>
        [Parameter]
        public IBrowserFile ImageFile { get; set; }
        /// <summary>
        /// Fire when the image file load into cropper
        /// </summary>
        /// <value></value>
        [Parameter]
        public EventCallback OnLoad { get; set; }
        /// <summary>
        /// Set whether the anime gif crop is enabled. If enabled,
        /// the gif file smaller than 1mb would 
        /// be cropped as animed image. If disabled, only the first
        /// frame would be cropped.
        /// </summary>
        /// <remarks>Crop large gif image can cause the window stop responding
        /// for half a minute!</remarks>
        /// <value>default: true</value>
        [Parameter]
        public bool AnimeGifEnable { get; set; } = true;
        /// <summary>
        /// The input element's id value. This param is optional and
        /// can help cropper to init image
        /// faster on browser.
        /// Does't have effect if <see cref="PureCSharpProcessing"/> is true.
        /// </summary>
        [Parameter]
        public string InputId { get; set; }
        /// <summary>
        /// Max allowed crop result height. Should not be larger than 500.
        /// </summary>
        /// <value>default:500</value>
        [Parameter]
        public double MaxCropedHeight { get; set; } = 500;
        /// <summary>
        /// Max allowed crop result width. Should not be larger than 500.
        /// </summary>
        /// <value>default:500</value>
        [Parameter]
        public double MaxCropedWidth { get; set; } = 500;
        /// <summary>
        /// Height of this component in px
        /// </summary>
        /// <value>default:150</value>
        [Parameter]
        public double CropperHeight { get; set; } = 150;
        /// <summary>
        /// Whether the cropper size is locked
        /// </summary>
        /// <value>default: false</value>
        [Parameter]
        public bool IsCropLocked { get; set; } = false;
        /// <summary>
        /// The scaling ratio, should be bind two way.
        /// </summary>
        /// <value>default: 1.0</value>
        [Parameter]
        public double Ratio
        {
            get => _ratio;
            set
            {
                if (value == _ratio||IsImageLocked)
                {
                    return;
                }

                _ratio = value;
                RatioChanged.InvokeAsync(value);
            }
        }
        /// <summary>
        /// Fire when scaling ratio changed by touch event.
        /// </summary>
        [Parameter]
        public EventCallback<double> RatioChanged { get; set; }
        /// <summary>
        /// Fire when cropper size changed. 
        /// Item1 of the return tuple is width, Item2 is height
        /// </summary>
        [Parameter]
        public EventCallback<(double, double)> OnSizeChanged { get; set; }
        #endregion


        #region public vars
        /// <summary>
        /// Max allowed scaling ratio
        /// </summary>
        /// <value></value>
        public double MaxRatio { get; private set; }
        /// <summary>
        /// Min allowed scaling ratio
        /// </summary>
        /// <value>1.0</value>
        public double MinRatio { get => 1.0; }
        #endregion


        #region private props
        private bool WiderThanContainer
        {
            get => (double)_image.Width / (double)_image.Height
                > _imgContainerWidth / _imgContainerHeight;
        }

        private double ImgRealW
        {
            get
            {
                if (WiderThanContainer)
                {
                    return _imgContainerWidth * _imgSize / 100;
                }
                else
                {
                    return _imgContainerHeight * _imgSize
                        / 100 * (double)_image.Width / _image.Height;
                }
            }
        }

        private double ImgRealH
        {
            get
            {
                if (WiderThanContainer)
                {
                    return _imgContainerWidth * _imgSize / 100
                        * (double)_image.Height / _image.Width;
                }
                else
                {
                    return _imgContainerHeight * _imgSize / 100;
                }
            }
        }

        private string BackgroundImgStyle
        {
            get => FormattableString.Invariant($"left:{_bacx}px;top:{_bacy}px;height:{_imgSize}%;");
        }
        private string ImglistStyle
        {
            get => FormattableString.Invariant($"height:{CropperHeight}px;");
        }
        #endregion


        #region vars
        private double _prevBacX = 0;
        private double _prevBacY = 0;
        private double _bacx = 0;
        private double _bacy = 0;
        private double _imgSize = 100;
        private bool _onTwoFingerResizing = false;
        private bool _isBacMoving = false;
        private double _prevTouchPointDistance = -1;
        private double _imgContainerWidth = 500;
        private double _imgContainerHeight = 150;
        private double _prevPosX = 0;
        private double _prevPosY = 0;
        private double _layoutX = 0;
        private double _layoutY = 0;
        private double _offsetX;
        private double _offsetY;
        private string _cropperStyle = "";
        private string _cropedImgStyle = "clip: rect(0, 150px, 150px, 0);";
        private bool _dragging = false;
        private bool _reSizing = false;
        private MoveDir _dir = MoveDir.UnKnown;
        private readonly double _minval = 30;
        private ImageData _image;
        private double _minposX;
        private double _minposY;
        private double _imgw;
        private double _imgh;
        private double _unsavedX;
        private double _unsavedY;
        private double _unsavedCropW;
        private double _unsavedCropH;
        private bool _outOfBox = false;
        private IBrowserFile _prevFile;
        private IImageFormat _format;
        private Image _gifimage;
        private double _ratio = 1d;
        private bool _evInitialized = false;

        #endregion


        #region static actions
        private static Action _setaction;
        private static Action<MouseEventArgs> _mouseMoveAction;
        private static Action<MouseEventArgs> _touchMoveAction;
        private static Action<MouseEventArgs> _touchEndAction;
        private static Action<MouseEventArgs> _mouseUpAction;
        #endregion



        #region Override methods
        protected override async Task OnInitializedAsync()
        {
            await JSRuntime.InvokeVoidAsync("setVersion", Environment.Version.Major);
        }
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (initCropWidth < 0)
            {
                initCropWidth = InitCropWidth;
                initCropHeight = InitCropHeight;
            }
        }
        public async ValueTask DisposeAsync()
        {
            await JSRuntime.InvokeVoidAsync("rmCropperEventListeners");
        }

        private double prevR = 1d;
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (Ratio != prevR && MinRatio <= Ratio && Ratio <= MaxRatio && !_onTwoFingerResizing)
            {
                prevR = Ratio;
                double temp = _imgSize;
                _imgSize = Ratio * 100;
                GuardImgPosition();
                _minposY += _bacy - (_imgSize - temp) / _imgSize * ImgRealH / 2;
                _minposX += _bacx;
                if (_prevPosX + initCropWidth > _minposX + ImgRealW || _prevPosX < _minposX
                    || _prevPosY + initCropHeight > _minposY + ImgRealH || _prevPosY < _minposY)
                {
                    _imgSize = temp;
                    Ratio = _imgSize / 100;
                }
                else
                {
                    _bacy -= (_imgSize - temp) / _imgSize * ImgRealH / 2;
                    _layoutY -= (_imgSize - temp) / _imgSize * ImgRealH / 2;
                    SetCroppedImgStyle();
                }
                InitBox();
            }
            if (RequireAspectRatio)
            {
                if (initCropHeight > initCropWidth * AspectRatio)
                {
                    initCropHeight = initCropWidth * AspectRatio;
                }
                else
                {
                    initCropWidth = initCropHeight / AspectRatio;
                }
                SetCropperStyle();
                SetCroppedImgStyle();
            }
            if (_prevFile == ImageFile)
            {
                return;
            }
            else
            {
                _prevFile = ImageFile;
            }
            string ext = ImageFile.Name.Split('.').Last().ToLower();
            IBrowserFile resizedImageFile = ImageFile;
            await Task.Delay(10);


            _gifimage?.Dispose();
            if (PureCSharpProcessing || string.IsNullOrEmpty(InputId) || (ext == "gif" && AnimeGifEnable))
            {
                byte[] buffer = new byte[resizedImageFile.Size];
                await resizedImageFile.OpenReadStream(100000000).ReadAsync(buffer);
                if ((ext == "gif" && AnimeGifEnable) || PureCSharpProcessing)
                {
                    _gifimage = Image.Load(buffer, out _format);
                    await JSRuntime.InvokeVoidAsync("setImgSrc", buffer, _format.DefaultMimeType);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setImgSrc", buffer, "image/" + ext);
                }
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("setImg", InputId);
            }

            double[] data = new double[] { 0, 0 };
            while (data[0] == 0d)
            {
                await Task.Delay(10);
                data = await JSRuntime.InvokeAsync<double[]>("getOriImgSize");
            }
            _image = new ImageData
            {
                Width = data[0],
                Height = data[1]
            };
            if (!_evInitialized)
            {
                await JSRuntime.InvokeVoidAsync("addCropperEventListeners");
                _evInitialized = true;
            }
            await SetImgContainterSize();
            MaxRatio = _imgContainerWidth / ImgRealW;
            Ratio = _imgSize / 100;
            await OnLoad.InvokeAsync();
            await SizeChanged();
        }
        protected override async Task OnAfterRenderAsync(bool first)
        {
            await base.OnAfterRenderAsync(first);
            if (first)
            {
                _setaction = () => SetImgContainterSize();
                _mouseMoveAction = args =>
                {
                    OnSizeChanging(args);
                    OnResizeBackGroundImage(MouseToTouch(args));
                };
                _mouseUpAction = args =>
                {
                    if (_onTwoFingerResizing || _isBacMoving)
                    {
                        _isBacMoving = false;
                        _onTwoFingerResizing = false;
                        InitBox();
                    }
                    OnSizeChangeEnd(args);
                };
                _touchMoveAction = OnSizeChanging;
                _touchEndAction = (args) =>
                {
                    _outOfBox = true;
                    OnSizeChangeEnd(args);
                };
            }
        }
        #endregion

        #region JsInvokable methods
        [JSInvokable("OnTouchEnd")]
        public static void TouchEndCaller(MouseEventArgs args)
        {
            _touchEndAction?.Invoke(args);
        }
        [JSInvokable("OnTouchMove")]
        public static void TouchMoveCaller(MouseEventArgs args)
        {
            _touchMoveAction?.Invoke(args);
        }
        [JSInvokable("OnMouseUp")]
        public static void MouseUpCaller(MouseEventArgs args)
        {
            _mouseUpAction?.Invoke(args);
        }
        [JSInvokable("OnMouseMove")]
        public static void MouseMoveCaller(MouseEventArgs args)
        {
            _mouseMoveAction?.Invoke(args);
        }
        [JSInvokable("SetWidthHeight")]
        public static void SetWidthHeightCaller()
        {
            _setaction?.Invoke();
        }
        #endregion


        #region Public methods
        /// <summary>
        /// Get the crop result.
        /// </summary>
        /// <returns>crop result</returns>
        public async Task<ImageCroppedResult> GetCropedResult()
        {
            int deltaX = 0;
            int deltaY = 0;
            double i = GetI();

            if (WiderThanContainer)
            {
                deltaY = -(int)(_imgContainerHeight / i - _image.Height) / 2;
            }
            else
            {
                deltaX = -(int)(_imgContainerWidth / i - _image.Width) / 2;
            }
            var (resizeProp,width,height) = GetCropperInfos(i);
            double x = ((_prevPosX - _bacx) / i + deltaX);
            double y = ((_prevPosY - _bacy) / i + deltaY);
            double proportionalCropWidth = (width * resizeProp);
            double proportionalCropHeight = (height * resizeProp);


            var rect = GetCropInfo();

            if (_gifimage == null)
            {
                if (Environment.Version.Major > 5)
                {
                    // for dotnet version after 5, pass byte array between c# and js is optimized
                    // async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format)
                    var bin = await JSRuntime.InvokeAsync<byte[]>("cropAsync", "oriimg", rect.X, rect.Y, rect.Width, rect.Height, 0, 0, (int)proportionalCropWidth, (int)proportionalCropHeight, "image/png");
                    return new ImageCroppedResult(bin);
                }
                else
                {
                    // async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format)
                    string s = await JSRuntime.InvokeAsync<string>("cropAsync", "oriimg", (int)x, (int)y, (int)(width), (int)(height), 0, 0, (int)proportionalCropWidth, (int)proportionalCropHeight, "image/png");
                    return new ImageCroppedResult(s);
                }
            }
            else
            {
                _gifimage.Mutate(ctx =>
                {
                    // fix exif orientaion issue
                    ctx.AutoOrient();
                    ctx.Crop(rect);
                    if (resizeProp != 1d)
                    {
                        ctx.Resize(new Size((int)proportionalCropWidth, (int)proportionalCropHeight));
                    }
                });
                return new ImageCroppedResult(_gifimage, _format);
            }
        }

        /// <summary>
        /// Returns the metadata about the desired cropping.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetCropInfo()
        {

            var i = GetI();
            var (_, cw, ch) = GetCropperInfos(i);

            int deltaX = 0;
            int deltaY = 0;

            if (WiderThanContainer)
            {
                deltaY = -(int)(_imgContainerHeight / i - _image.Height) / 2;
            }
            else
            {
                deltaX = -(int)(_imgContainerWidth / i - _image.Width) / 2;
            }

            double x = ((_prevPosX - _bacx) / i + deltaX);
            double y = ((_prevPosY - _bacy) / i + deltaY);


            return new Rectangle((int) (i*x), (int)(i*y), (int)(cw*i), (int)(ch*i));
        }

        #endregion


        #region private methods

        private string GetCropperStyle(double top, double left, double height, double width)
        {
            return FormattableString.Invariant($"top:{top}px;left:{left}px;height:{height}px;width:{width}px;");
        }

        private string GetCroppedImgStyle(double top, double right, double bottom, double left)
        {
            return FormattableString.Invariant($"clip: rect({top}px, {right}px, {bottom}px, {left}px);");
        }

        private (double resizeProp,double cw,double ch) GetCropperInfos(double i)
        {
            double resizeProp = 1d;
            double cw = (initCropWidth / i);
            double ch = (initCropHeight / i);
            if (cw > MaxCropedWidth || ch > MaxCropedHeight)
            {
                if (MaxCropedWidth / MaxCropedHeight > (double)cw / (double)ch)
                {
                    resizeProp = MaxCropedHeight / ch;
                }
                else
                {
                    resizeProp = MaxCropedWidth / cw;
                }
            }
            return (resizeProp, cw, ch);
        }

        private double GetI()
        {
            double i = 0d;
            if (WiderThanContainer)
            {
                double containerWidth = _imgContainerWidth * _imgSize / 100;
                i = containerWidth / _image.Width;
            }
            else
            {
                double containerHeight = _imgContainerHeight * _imgSize / 100;
                i = containerHeight / _image.Height;
            }
            return i;
        }

        private void SetCroppedImgStyle()
        {
            _cropedImgStyle = GetCroppedImgStyle(_prevPosY - _layoutY, _prevPosX - _layoutX + initCropWidth, _prevPosY - _layoutY + initCropHeight, _prevPosX - _layoutX);
        }

        private void SetCropperStyle()
        {
            _cropperStyle = GetCropperStyle(_prevPosY, _prevPosX, initCropHeight, initCropWidth);
        }

        private MouseEventArgs TouchToMouse(TouchEventArgs args)
        {
            try
            {
                return new MouseEventArgs()
                {
                    ClientX = args.Touches[0].ClientX,
                    ClientY = args.Touches[0].ClientY
                };
            }
            catch (System.Exception)
            {
                _outOfBox = true;
                return new MouseEventArgs();
            }
        }

        private void OnDragStart(MouseEventArgs args)
        {
            _outOfBox = false;
            if (_reSizing)
            {
                return;
            }
            SetCropperStyle();
            _offsetX = args.ClientX;
            _offsetY = args.ClientY;
            _dragging = true;
        }

        private void OnDragging(MouseEventArgs args)
        {
            if (_dragging && !_reSizing)
            {
                double x = _prevPosX - _offsetX + args.ClientX;
                double y = _prevPosY - _offsetY + args.ClientY;
                if (y < _minposY)
                {
                    _outOfBox = true;
                    y = _minposY;
                }
                if (x < _minposX)
                {
                    _outOfBox = true;
                    x = _minposX;
                }
                if (y + initCropHeight > (_minposY + _imgh))
                {
                    _outOfBox = true;
                    y = (_minposY + _imgh) - initCropHeight;
                }
                if (x + initCropWidth > (_minposX + _imgw))
                {
                    _outOfBox = true;
                    x = (_minposX + _imgw) - initCropWidth;
                }
                _unsavedX = x;
                _unsavedY = y;

                _cropperStyle = GetCropperStyle(y, x, initCropHeight, initCropWidth);
                _cropedImgStyle = GetCroppedImgStyle(y - _layoutY, x - _layoutX + initCropWidth, y - _layoutY + initCropHeight, x - _layoutX);
                base.StateHasChanged();
            }
        }

        private void OnDragEnd(MouseEventArgs args)
        {
            _dragging = false;
            OnDragging(args);
            if (_outOfBox)
            {
                _prevPosX = _unsavedX;
                _prevPosY = _unsavedY;
                return;
            }
            _prevPosX = _prevPosX - _offsetX + args.ClientX;
            _prevPosY = _prevPosY - _offsetY + args.ClientY;
        }

        private void OnResizeStart(MouseEventArgs args, MoveDir dir)
        {
            _outOfBox = false;
            this._dir = dir;
            if (_dragging)
            {
                return;
            }
            SetCropperStyle();
            _offsetX = args.ClientX;
            _offsetY = args.ClientY;
            if (IsCropLocked && !_isBacMoving && !_onTwoFingerResizing)
            {
                OnDragStart(args);
                return;
            }
            _reSizing = true;
        }

        private void OnSizeChanging(MouseEventArgs args)
        {
            if (_reSizing && !_dragging)
            {
                double delta = args.ClientY - _offsetY;
                double deltaX = args.ClientX - _offsetX;
                double ytemp = _prevPosY;
                double tempCropHeight = initCropHeight;
                double xtemp = _prevPosX;
                double tempCropWidth = initCropWidth;
                void xxLeft()
                {
                    tempCropHeight = initCropHeight +
                        ((_dir == MoveDir.UpLeft) ? (-delta) : delta);
                    tempCropWidth = initCropWidth - deltaX;
                    if (RequireAspectRatio)
                    {
                        tempCropWidth = tempCropHeight / AspectRatio;
                        deltaX = initCropWidth - tempCropWidth;
                    }
                    xtemp = _prevPosX + deltaX;
                };
                switch (_dir)
                {
                    case MoveDir.Up:
                        {
                            ytemp = _prevPosY + delta;
                            tempCropHeight = initCropHeight - delta;
                            break;
                        }
                    case MoveDir.Down:
                        {
                            tempCropHeight = initCropHeight + delta;
                            break;
                        }
                    case MoveDir.Left:
                        {
                            xtemp = _prevPosX + deltaX;
                            tempCropWidth = initCropWidth - deltaX;
                            break;
                        }
                    case MoveDir.Right:
                        {
                            tempCropWidth = initCropWidth + deltaX;
                            break;
                        }
                    case MoveDir.UpLeft:
                        {
                            ytemp = _prevPosY + delta;
                            xxLeft();
                            break;
                        }
                    case MoveDir.UpRight:
                        {
                            ytemp = _prevPosY + delta;
                            tempCropHeight = initCropHeight - delta;
                            tempCropWidth = initCropWidth + deltaX;
                            break;
                        }
                    case MoveDir.DownLeft:
                        {
                            xxLeft();
                            break;
                        }
                    case MoveDir.DownRight:
                        {
                            tempCropHeight = initCropHeight + delta;
                            tempCropWidth = initCropWidth + deltaX;
                            break;
                        }
                    default:
                        break;
                }
                if (RequireAspectRatio)
                {
                    _ = _dir switch
                    {
                        MoveDir.Left or MoveDir.Right =>
                            tempCropHeight = tempCropWidth * AspectRatio,
                        MoveDir.UpLeft or MoveDir.DownLeft => 0,
                        MoveDir.UnKnown => throw new NotImplementedException(),
                        _ => tempCropWidth = tempCropHeight / AspectRatio
                    };
                }
                if (ytemp < _minposY)
                {
                    _outOfBox = true;
                    ytemp = _minposY;
                }
                if (xtemp < _minposX)
                {
                    _outOfBox = true;
                    xtemp = _minposX;
                }
                if (ytemp + tempCropHeight > (_minposY + _imgh))
                {
                    _outOfBox = true;
                    tempCropHeight = (_minposY + _imgh) - ytemp;
                    if (RequireAspectRatio)
                    {
                        tempCropWidth = tempCropHeight / AspectRatio;
                    }
                }
                if (xtemp + tempCropWidth > (_minposX + _imgw))
                {
                    _outOfBox = true;
                    tempCropWidth = (_minposX + _imgw) - xtemp;
                    if (RequireAspectRatio)
                    {
                        tempCropHeight = tempCropWidth / AspectRatio;
                    }
                }
                void CheckMin(ref double len)
                {
                    if (len < _minval)
                    {
                        len = _minval;
                        ytemp = _unsavedY;
                        xtemp = _unsavedX;
                    }
                }
                CheckMin(ref tempCropHeight);
                CheckMin(ref tempCropWidth);
                _unsavedX = xtemp;
                _unsavedY = ytemp;
                _unsavedCropH = tempCropHeight;
                _unsavedCropW = tempCropWidth;
                _cropperStyle = GetCropperStyle(ytemp, xtemp, tempCropHeight, tempCropWidth);
                _cropedImgStyle = GetCroppedImgStyle(ytemp - _layoutY, xtemp - _layoutX + tempCropWidth, ytemp - _layoutY + tempCropHeight, xtemp - _layoutX);
            }
            OnDragging(args);
            base.StateHasChanged();
        }

        private void OnSizeChangeEnd(MouseEventArgs args)
        {
            if (_reSizing)
            {
                _reSizing = false;
                OnSizeChanging(args);
                double delta = args.ClientY - _offsetY;
                double deltaX = args.ClientX - _offsetX;
                if (_outOfBox)
                {
                    initCropHeight = _unsavedCropH;
                    initCropWidth = _unsavedCropW;
                    _prevPosY = _unsavedY;
                    _prevPosX = _unsavedX;
                    return;
                }
                switch (_dir)
                {
                    case MoveDir.Up:
                        {
                            _prevPosY = _prevPosY + delta;
                            initCropHeight -= delta;
                            break;
                        }
                    case MoveDir.Down:
                        {
                            initCropHeight += delta;
                            break;
                        }
                    case MoveDir.Left:
                        {
                            _prevPosX += deltaX;
                            initCropWidth -= deltaX;
                            break;
                        }
                    case MoveDir.Right:
                        {
                            initCropWidth += deltaX;
                            break;
                        }
                    case MoveDir.UpLeft:
                        {
                            _prevPosY = _prevPosY + delta;
                            initCropHeight -= delta;
                            break;
                        }
                    case MoveDir.DownLeft:
                        {
                            initCropHeight += delta;
                            break;
                        }
                    case MoveDir.UpRight:
                        {
                            _prevPosY = _prevPosY + delta;
                            initCropHeight -= delta;
                            initCropWidth += deltaX;
                            break;
                        }
                    case MoveDir.DownRight:
                        {
                            initCropHeight += delta;
                            initCropWidth += deltaX;
                            break;
                        }
                    default:
                        break;
                }
                double xxLeft()
                {
                    double ori = initCropWidth;
                    initCropWidth = initCropHeight / AspectRatio;
                    deltaX = ori - initCropWidth;
                    _prevPosX += deltaX;
                    return 0;
                }
                if (RequireAspectRatio)
                {
                    _ = _dir switch
                    {
                        MoveDir.Left or MoveDir.Right =>
                            initCropHeight = initCropWidth * AspectRatio,
                        MoveDir.UpLeft or MoveDir.DownLeft =>xxLeft(),
                        MoveDir.UnKnown => throw new NotImplementedException(),
                        _ => initCropWidth = initCropHeight / AspectRatio
                    };
                }
                else if (!RequireAspectRatio&&(_dir is MoveDir.UpLeft or MoveDir.DownLeft))
                {
                    _prevPosX += deltaX;
                    initCropWidth -= deltaX;
                }
                if (initCropHeight < _minval)
                {
                    initCropHeight = _minval;
                }
                if (initCropWidth < _minval)
                {
                    initCropWidth = _minval;
                }
            }
            if (_dragging)
            {
                OnDragEnd(args);
            }
            SizeChanged();
        }

        private Task SizeChanged()
        {
            double i = GetI();
            var (resizeProp, cw, ch) = GetCropperInfos(i);
            return OnSizeChanged.InvokeAsync((cw * resizeProp, ch * resizeProp));
        }

        private TouchEventArgs MouseToTouch(MouseEventArgs args)
        {
            return new TouchEventArgs
            {
                Touches = new[]{
                new TouchPoint{
                ClientX = args.ClientX,
                ClientY = args.ClientY
            }
            }
            };
        }
        private void GuardImgPosition()
        {
            if (WiderThanContainer)
            {
                _minposX = 0;
                _minposY = (_imgContainerHeight - ImgRealH) / 2;
            }
            else
            {
                _minposY = 0;
                _minposX = (_imgContainerWidth - ImgRealW) / 2;
            }
        }
        private void ResizeBac(double i)
        {
            double temp = _imgSize;
            if (_imgSize * i < 100 && i < 1 ||
                ((ImgRealW * i > _imgContainerWidth) && (ImgRealH * i > _imgContainerHeight) && i > 1))
                return;
            _imgSize *= i;
            GuardImgPosition();
            _minposY += _bacy - (_imgSize - temp) / _imgSize * ImgRealH / 2;
            _minposX += _bacx;
            if (_prevPosX + initCropWidth > _minposX + ImgRealW || _prevPosX < _minposX
                || _prevPosY + initCropHeight > _minposY + ImgRealH || _prevPosY < _minposY)
                _imgSize /= i;
            else
            {
                _bacy -= (i - 1) * ImgRealH / 2;
                _layoutY -= (i - 1) * ImgRealH / 2;
                SetCroppedImgStyle();
            }
        }

        private void OnResizeBackGroundImage(TouchEventArgs args)
        {
            if (args.Touches.Length == 1)
            {
                if (!_isBacMoving)
                {
                    return;
                }
                double dx = args.Touches[0].ClientX - _prevBacX;
                double dy = args.Touches[0].ClientY - _prevBacY;
                GuardImgPosition();
                _minposY += _bacy + dy;
                _minposX += _bacx + dx;
                _bacx += dx;
                _bacy += dy;
                _layoutX += dx;
                _layoutY += dy;
                if (_prevPosX + initCropWidth > _minposX + ImgRealW || _prevPosX < _minposX)
                {
                    _bacx -= dx;
                    _layoutX -= dx;
                    _minposX -= _bacx;
                }
                if (_prevPosY + initCropHeight > _minposY + ImgRealH || _prevPosY < _minposY)
                {
                    _bacy -= dy;
                    _layoutY -= dy;
                    _minposY -= _bacy;
                }
                SetCroppedImgStyle();
                _prevBacX = args.Touches[0].ClientX;
                _prevBacY = args.Touches[0].ClientY;
            }
            if (args.Touches.Length != 2||IsImageLocked)
            {
                return;
            }
            // two finger resize
            double distance = Math.Pow((args.Touches[0].ClientX - args.Touches[1].ClientX), 2) +
                Math.Pow((args.Touches[0].ClientY - args.Touches[1].ClientY), 2);
            if (_onTwoFingerResizing)
            {
                double i = distance / _prevTouchPointDistance;
                ResizeBac(i);
                Ratio = _imgSize / 100;
            }
            else
            {
                _onTwoFingerResizing = true;
            }
            _prevTouchPointDistance = distance;

        }

        private async Task SetImgContainterSize()
        {
            double[] t = await JSRuntime.InvokeAsync<double[]>("getWidthHeight");
            // in the case that container has not yet loaded 
            while (t[0] == 0)
            {
                await Task.Delay(10);
                t = await JSRuntime.InvokeAsync<double[]>("getWidthHeight");
            }
            _imgContainerWidth = t[0];
            _imgContainerHeight = t[1];
            InitStyles();
        }

        private void InitBox()
        {
            double prevPosX = 0d, prevPosY = 0d;

            InitPos(ref prevPosX, ref prevPosY);
        }
        private void InitPos(ref double prevPosX, ref double prevPosY)
        {
            if (WiderThanContainer)
            {
                prevPosY = (_imgContainerHeight - ImgRealH) / 2;
                prevPosX = 0;
            }
            else
            {
                prevPosX = Math.Abs(_imgContainerWidth - ImgRealW) / 2;
                prevPosY = 0;
            }
            prevPosY += _bacy;
            prevPosX += _bacx;
            _minposX = prevPosX;
            _minposY = prevPosY;
            _imgw = ImgRealW;
            _imgh = ImgRealH;
        }

        [Parameter]
        public double OffsetX { set; get; }

        [Parameter]
        public double OffsetY { set; get; }

        private void InitStyles()
        {
            InitPos(ref _prevPosX, ref _prevPosY);
            _prevPosX += OffsetX;
            _prevPosY += OffsetY;

            if (initCropHeight > _imgh)
            {
                initCropHeight = _imgh;
            }
            if (initCropWidth > _imgw)
            {
                initCropWidth = _imgw;
            }

            SetCropperStyle();
            SetCroppedImgStyle();
            base.StateHasChanged();
        }
        #endregion
    }
}
