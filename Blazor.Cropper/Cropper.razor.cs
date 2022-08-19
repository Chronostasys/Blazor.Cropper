using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Blazor.Cropper
{
    public partial class Cropper : IAsyncDisposable
    {
        [Inject]
        internal IJSRuntime JSRuntime { get; set; }

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
        /// shall not be smaller than 30.
        /// </summary>
        /// <remarks>It's unit is not pixel. For more info, see <seealso cref="CropInfo.GetInitParams"/></remarks>
        /// <value>default: 150</value>
        [Parameter]
        public double InitCropWidth { get; set; } = 150;

        internal double initCropWidth = -1;
        /// <summary>
        /// the initial height of cropper if possible.
        /// shall not be smaller than 30.
        /// </summary>
        /// <remarks>It's unit is not pixel. For more info, see <seealso cref="CropInfo.GetInitParams"/></remarks>
        /// <value>default: 150</value>
        [Parameter]
        public double InitCropHeight { get; set; } = 150;

        internal double initCropHeight = -1;
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
        /// faster on browser before dotnet core version 6.
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
                if (value == _ratio || IsImageLocked)
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
        /// <summary>
        /// Used to set the cropper initial offset position.
        /// </summary>
        /// <remarks>It's unit is not pixel. For more info, see <seealso cref="CropInfo.GetInitParams"/></remarks>
        [Parameter]
        public double OffsetX { set; get; }
        /// <summary>
        /// Used to set the cropper initial offset position.
        /// </summary>
        /// <remarks>It's unit is not pixel. For more info, see <seealso cref="CropInfo.GetInitParams"/></remarks>
        [Parameter]
        public double OffsetY { set; get; }
        /// <summary>
        /// Quality of output image. Should be between 0 and 100.
        /// Only takes effect when image is in jpg or webp format.
        /// </summary>
        /// <value>100</value>
        [Parameter]
        public int Quality { set; get; } = 100;
        #endregion


        #region public vars
        /// <summary>
        /// Min allowed scaling ratio
        /// </summary>
        /// <value>1.0</value>
        public double MinRatio => 1.0;

        /// <summary>
        /// Max allowed scaling ratio
        /// </summary>
        /// <remarks>This property is obsolete. Max ratio is no longer limited</remarks>
        /// <value>2</value>
        [Obsolete("This property is obsolete. Max ratio is no longer limited")]
        public double MaxRatio => 2.0;

        #endregion


        #region internal props
        internal bool WiderThanContainer
        {
            get => (double)_image.Width / (double)_image.Height
                > _imgContainerWidth / _imgContainerHeight;
        }

        internal double ImgRealW
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

        internal double ImgRealH
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

        internal string BackgroundImgStyle
        {
            get => FormattableString.Invariant($"left:{_bacx}px;top:{_bacy}px;");
        }
        internal string RatioStyle
        {
            get => FormattableString.Invariant($"transform:scale({Ratio});");
        }
        internal string ImglistStyle
        {
            get => FormattableString.Invariant($"height:{CropperHeight}px;");
        }
        #endregion


        #region vars
        internal double _prevBacX = 0;
        internal double _prevBacY = 0;
        internal double _bacx = 0;
        internal double _bacy = 0;
        internal double _imgSize = 100;
        internal bool _onTwoFingerResizing = false;
        internal bool _isBacMoving = false;
        internal double _prevTouchPointDistance = -1;
        internal double _imgContainerWidth = 500;
        internal double _imgContainerHeight = 150;
        internal double _prevPosX = 0;
        internal double _prevPosY = 0;
        internal double _layoutX = 0;
        internal double _layoutY = 0;
        internal double _offsetX;
        internal double _offsetY;
        internal string _cropperStyle = "";
        internal string _cropedImgStyle = "clip: rect(0, 150px, 150px, 0);";
        internal bool _dragging = false;
        internal bool _reSizing = false;
        internal MoveDir _dir = MoveDir.UnKnown;
        internal readonly double _minval = 30;
        internal ImageData _image;
        internal double _minposX;
        internal double _minposY;
        internal double _imgw;
        internal double _imgh;
        internal double _unsavedX;
        internal double _unsavedY;
        internal double _unsavedCropW;
        internal double _unsavedCropH;
        internal IBrowserFile _prevFile;
        internal IImageFormat _format;
        internal Image _gifimage;
        internal double _ratio = 1d;
        internal bool _evInitialized = false;
        internal double _minvalx;
        internal double _minvaly;

        #endregion


        #region static actions
        internal static Action _setaction;
        internal static Action<MouseEventArgs> _mouseMoveAction;
        internal static Action<MouseEventArgs> _touchMoveAction;
        internal static Action<MouseEventArgs> _touchEndAction;
        internal static Action<MouseEventArgs> _mouseUpAction;
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

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            _minvalx = _minval;
            _minvaly = _minval;
            if (RequireAspectRatio)
            {
                if (initCropHeight > initCropWidth * AspectRatio)
                {
                    initCropWidth = initCropHeight / AspectRatio;
                    _minvalx = _minvaly/AspectRatio;
                }
                else
                {
                    initCropHeight = initCropWidth * AspectRatio;
                    _minvaly = _minvalx * AspectRatio;
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
            await LoadImage(ext, resizedImageFile);

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

            var info = GetCropInfo();
            var rect = info.Rectangle;
            var (x, y, proportionalCropWidth, proportionalCropHeight, resizeProp) = (rect.X, rect.Y, rect.Width, rect.Height, info.ResizeProp);
            if (_gifimage == null)
            {
                if (Environment.Version.Major > 5)
                {
                    // for dotnet version after 5, pass byte array between c# and js is optimized
                    // async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format)
                    var bin = await JSRuntime.InvokeAsync<byte[]>("cropAsync", "oriimg",
                        x, y, (proportionalCropWidth), (proportionalCropHeight), 0, 0,
                        proportionalCropWidth, proportionalCropHeight, _format.DefaultMimeType, Quality);
                    return new ImageCroppedResult(bin, _format);
                }
                else
                {
                    // async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format)
                    string s = await JSRuntime.InvokeAsync<string>("cropAsync", "oriimg",
                        x, y, (proportionalCropWidth), (proportionalCropHeight), 0, 0,
                        proportionalCropWidth, proportionalCropHeight, _format.DefaultMimeType, Quality);
                    return new ImageCroppedResult(s, _format);
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
                        ctx.Resize(new Size(proportionalCropWidth, proportionalCropHeight));
                    }
                });
                return new ImageCroppedResult(_gifimage, _format, Quality);
            }
        }

        /// <summary>
        /// Returns the metadata about the cropper state.
        /// </summary>
        /// <returns></returns>
        public CropInfo GetCropInfo()
        {
            double deltaX = 0;
            double deltaY = 0;
            var (resizeProp, width, height) = GetCropperInfos();
            if (WiderThanContainer)
            {
                deltaY = -(_imgContainerHeight - _image.Height / resizeProp) / 2;
            }
            else
            {
                deltaX = -(_imgContainerWidth - _image.Width / resizeProp) / 2;
            }
            double x = ((_prevPosX - _bacx) + deltaX);
            double y = ((_prevPosY - _bacy) + deltaY);


            return new CropInfo
            {
                Rectangle = new Rectangle((int)(x * resizeProp), (int)(y * resizeProp),
                (int)(width * resizeProp), (int)(height * resizeProp)),
                Ratio = Ratio,
                ResizeProp = resizeProp
            };
        }

        #endregion


        #region internal methods

        internal string GetCropperStyle(double top, double left, double height, double width)
        {
            return FormattableString.Invariant($"top:{top}px;left:{left}px;height:{height}px;width:{width}px;");
        }

        internal string GetCroppedImgStyle(double top, double right, double bottom, double left)
        {
            return FormattableString.Invariant($"clip: rect({top}px, {right}px, {bottom}px, {left}px);");
        }

        internal (double resizeProp, double cw, double ch) GetCropperInfos()
        {
            double resizeProp = 1d;
            double cw = (initCropWidth);
            double ch = (initCropHeight);
            if (WiderThanContainer)
            {
                resizeProp = _image.Width / _imgContainerWidth;
            }
            else
            {
                resizeProp = _image.Height / _imgContainerHeight;
            }
            return (resizeProp, cw, ch);
        }


        internal void SetCroppedImgStyle()
        {
            _cropedImgStyle = GetCroppedImgStyle(_prevPosY - _layoutY, _prevPosX - _layoutX + initCropWidth, _prevPosY - _layoutY + initCropHeight, _prevPosX - _layoutX);
        }

        internal void SetCropperStyle()
        {
            _cropperStyle = GetCropperStyle(_prevPosY, _prevPosX, initCropHeight, initCropWidth);
        }

        internal void TouchToMouse(TouchEventArgs args, Action<MouseEventArgs> handler)
        {
            if (args != null && args.Touches != null && args.Touches.Length > 0)
            {
                handler(new MouseEventArgs
                {
                    ClientX = args.Touches[0].ClientX,
                    ClientY = args.Touches[0].ClientY
                });
            }
        }

        internal void OnDragStart(MouseEventArgs args)
        {
            if (_reSizing)
            {
                return;
            }
            SetCropperStyle();
            _offsetX = args.ClientX;
            _offsetY = args.ClientY;
            _dragging = true;
        }

        internal void OnDragging(MouseEventArgs args)
        {
            if (_dragging && !_reSizing)
            {
                double x = _prevPosX - (_offsetX - args.ClientX) / Ratio;
                double y = _prevPosY - (_offsetY - args.ClientY) / Ratio;
                if (y < _minposY)
                {
                    y = _minposY;
                }
                if (x < _minposX)
                {
                    x = _minposX;
                }
                if (y + initCropHeight > (_minposY + _imgh))
                {
                    y = (_minposY + _imgh) - initCropHeight;
                }
                if (x + initCropWidth > (_minposX + _imgw))
                {
                    x = (_minposX + _imgw) - initCropWidth;
                }
                _unsavedX = x;
                _unsavedY = y;

                _cropperStyle = GetCropperStyle(y, x, initCropHeight, initCropWidth);
                _cropedImgStyle = GetCroppedImgStyle(y - _layoutY, x - _layoutX + initCropWidth, y - _layoutY + initCropHeight, x - _layoutX);
                base.StateHasChanged();
            }
        }

        internal void OnDragEnd(MouseEventArgs args)
        {
            _dragging = false;
            OnDragging(args);
            _prevPosX = _unsavedX;
            _prevPosY = _unsavedY;
        }

        internal void OnResizeStart(MouseEventArgs args, MoveDir dir)
        {
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

        internal async ValueTask LoadImage(string ext, IBrowserFile resizedImageFile)
        {
            if (PureCSharpProcessing || string.IsNullOrEmpty(InputId) || (ext == "gif" && AnimeGifEnable))
            {
                byte[] buffer = new byte[resizedImageFile.Size];
                if (Environment.Version.Major == 6)
                {
                    var isClientSide = JSRuntime is IJSInProcessRuntime;
                    if (isClientSide)
                    {
                        await resizedImageFile.OpenReadStream(1024 * 1024 * 90).ReadAsync(buffer);
                    }
                    else
                    {
                        // WORKAROUND https://github.com/Chronostasys/Blazor.Cropper/issues/43
                        var fileContent = new StreamContent(resizedImageFile.OpenReadStream(1024 * 1024 * 90));
                        buffer = await fileContent.ReadAsByteArrayAsync();
                    }
                }
                else
                {
                    await resizedImageFile.OpenReadStream(1024 * 1024 * 90).ReadAsync(buffer);
                }
                if ((ext == "gif" && AnimeGifEnable) || PureCSharpProcessing)
                {
                    _gifimage = Image.Load(buffer, out _format);
                    await JSRuntime.InvokeVoidAsync("setImgSrc", buffer, _format.DefaultMimeType);
                }
                else
                {

                    var m = Configuration.Default.ImageFormatsManager;

                    _format = m.FindFormatByFileExtension(ext) ?? PngFormat.Instance;
                    await JSRuntime.InvokeVoidAsync("setImgSrc", buffer, "image/" + ext);
                }
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("setImg", InputId);
            }
        }

        internal void OnSizeChanging(MouseEventArgs args)
        {
            if (_reSizing && !_dragging)
            {
                double deltaY = (args.ClientY - _offsetY) / Ratio;
                double deltaX = (args.ClientX - _offsetX) / Ratio;
                var cb = new CropBox(this);
                cb.DragCorner(deltaX, deltaY,_dir);
                _unsavedX = cb.X;
                _unsavedY = cb.Y;
                _unsavedCropH = cb.H;
                _unsavedCropW = cb.W;
                _cropperStyle = GetCropperStyle(cb.Y, cb.X, cb.H, cb.W);
                _cropedImgStyle = GetCroppedImgStyle(cb.Y - _layoutY, cb.X - _layoutX + cb.W, cb.Y - _layoutY + cb.H, cb.X - _layoutX);
            }
            OnDragging(args);
            base.StateHasChanged();
        }

        internal void OnSizeChangeEnd(MouseEventArgs args)
        {
            if (_reSizing)
            {
                _reSizing = false;
                OnSizeChanging(args);
                initCropHeight = _unsavedCropH;
                initCropWidth = _unsavedCropW;
                _prevPosY = _unsavedY;
                _prevPosX = _unsavedX;
                SizeChanged();
                return;
            }
            if (_dragging)
            {
                OnDragEnd(args);
            }
            SizeChanged();
        }

        internal Task SizeChanged()
        {
            var (resizeProp, cw, ch) = GetCropperInfos();
            return OnSizeChanged.InvokeAsync((cw * resizeProp, ch * resizeProp));
        }

        internal TouchEventArgs MouseToTouch(MouseEventArgs args)
        {
            return new TouchEventArgs
            {
                Touches = new[]
                {
                    new TouchPoint
                    {
                        ClientX = args.ClientX,
                        ClientY = args.ClientY
                    }
                }
            };
        }
        internal void GuardImgPosition()
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
        internal void ResizeBac(double i)
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

        internal void OnResizeBackGroundImage(TouchEventArgs args)
        {
            if (args.Touches.Length == 1)
            {
                if (!_isBacMoving)
                {
                    return;
                }
                double dx = (args.Touches[0].ClientX - _prevBacX) / Ratio;
                double dy = (args.Touches[0].ClientY - _prevBacY) / Ratio;
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
            if (args.Touches.Length != 2 || IsImageLocked)
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

        internal async Task SetImgContainterSize()
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

        internal void InitBox()
        {
            double prevPosX = 0d, prevPosY = 0d;

            InitPos(ref prevPosX, ref prevPosY);
        }
        internal void InitPos(ref double prevPosX, ref double prevPosY)
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

        internal void InitStyles()
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
