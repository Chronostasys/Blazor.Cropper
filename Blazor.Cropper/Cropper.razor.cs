using System;
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
    public partial class Cropper
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
        /// the initial width of cropper if possible
        /// </summary>
        /// <value>default: 150</value>
        [Parameter]
        public double InitCropWidth { get; set; } = 150;

        private double initCropWidth = -1;
        /// <summary>
        /// the initial height of cropper if possible
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
        /// <remarks>Only works when <see cref="RequireAspectRatio"/> is true</remarks>
        /// <value>default: 1</value>
        [Parameter]
        public double AspectRatio { get; set; } = 1;
        /// <summary>
        /// The input image file. Usually get from an <see cref="InputFile"/> Component. You can also
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
        /// Set whether the anime gif crop is enabled. If enabled, the gif file smaller than 1mb would 
        /// be cropped as animed image. If disabled, only the first frame would be cropped.
        /// </summary>
        /// <remarks>Resizing gif image can cause the window stop responding for half a minute!</remarks>
        /// <value>default: true</value>
        [Parameter]
        public bool AnimeGifEnable { get; set; } = true;
        /// <summary>
        /// The input element's id value. This param is optional and can help cropper to init image
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
            get => ratio;
            set
            {
                if (value == ratio)
                {
                    return;
                }

                ratio = value;
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
        private bool widerThanContainer
        {
            get => (double)image.Width / (double)image.Height > imgContainerWidth / imgContainerHeight;
        }

        private double imgRealW
        {
            get
            {
                if (widerThanContainer)
                {
                    return imgContainerWidth * imgSize / 100;
                }
                else
                {
                    return imgContainerHeight * imgSize / 100 * (double)image.Width / image.Height;
                    // deltaY = -(int)(imgContainerHeight / i - image.Height) / 2; 
                }
            }
        }

        private double imgRealH
        {
            get
            {
                if (widerThanContainer)
                {
                    return imgContainerWidth * imgSize / 100 * (double)image.Height / image.Width;
                }
                else
                {
                    return imgContainerHeight * imgSize / 100;
                }
            }
        }

        private string backgroundImgStyle
        {
            get => $"left:{bacx}px;top: {bacy}px;height:{imgSize}%;";
        }
        private string imglistStyle
        {
            get => $"height:{CropperHeight}px;";
        }
        #endregion


        #region vars
        private double prevBacX = 0;
        private double prevBacY = 0;
        private double bacx = 0;
        private double bacy = 0;
        private double imgSize = 100;
        private bool onTwoFingerResizing = false;
        private bool isBacMoving = false;
        private double prevTouchPointDistance = -1;
        private double imgContainerWidth = 500;
        private double imgContainerHeight = 150;
        private double prevPosX = 0;
        private double prevPosY = 0;
        private double layoutX = 0;
        private double layoutY = 0;
        private double offsetX;
        private double offsetY;
        private string cropperStyle = "";
        private string cropedImgStyle = "clip: rect(0, 150px, 150px, 0);";
        private bool dragging = false;
        private bool reSizing = false;
        private MoveDir dir = MoveDir.UnKnown;
        private readonly double minval = 30;
        private ImageData image;
        private double minposX;
        private double minposY;
        private double imgw;
        private double imgh;
        private double unsavedX;
        private double unsavedY;
        private double unsavedCropW;
        private double unsavedCropH;
        private bool outOfBox = false;
        private IBrowserFile prevFile;
        private IImageFormat format;
        private Image gifimage;
        private double ratio = 1d;

        #endregion


        #region static actions
        private static Action setaction;
        private static Action<MouseEventArgs> mouseMoveAction;
        private static Action<MouseEventArgs> touchMoveAction;
        private static Action<MouseEventArgs> touchEndAction;
        private static Action<MouseEventArgs> mouseUpAction;
        #endregion



        #region Override methods
        protected override async Task OnInitializedAsync()
        {
            await JSRuntime.InvokeVoidAsync("addCropperEventListeners");
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

        private double prevR = 1d;
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (Ratio != prevR && MinRatio <= Ratio && Ratio <= MaxRatio && !onTwoFingerResizing)
            {
                prevR = Ratio;
                double temp = imgSize;
                imgSize = Ratio * 100;
                if (widerThanContainer)
                {
                    minposX = 0;
                    minposY = (imgContainerHeight - imgRealH) / 2;
                }
                else
                {
                    minposY = 0;
                    minposX = (imgContainerWidth - imgRealW) / 2;
                }
                minposY += bacy - (imgSize - temp) / imgSize * imgRealH / 2;
                minposX += bacx;
                if (prevPosX + initCropWidth > minposX + imgRealW || prevPosX < minposX
                    || prevPosY + initCropHeight > minposY + imgRealH || prevPosY < minposY)
                {
                    imgSize = temp;
                    Ratio = imgSize / 100;
                }
                else
                {
                    bacy -= (imgSize - temp) / imgSize * imgRealH / 2;
                    layoutY -= (imgSize - temp) / imgSize * imgRealH / 2;
                    SetCroppedImgStyle();
                }
                // SetCropperStyle();
                InitBox();
                ////prevPosY = bacy + prevPosY - layoutY;
                ////prevPosX = prevPosX - layoutX + bacx;
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
            if (prevFile == ImageFile)
            {
                return;
            }
            else
            {
                prevFile = ImageFile;
            }
            string ext = ImageFile.Name.Split('.').Last().ToLower();
            IBrowserFile resizedImageFile = ImageFile;
            await Task.Delay(10);


            gifimage?.Dispose();
            if (PureCSharpProcessing || string.IsNullOrEmpty(InputId) || (ext == "gif" && AnimeGifEnable))
            {
                byte[] buffer = new byte[resizedImageFile.Size];
                await resizedImageFile.OpenReadStream(100000000).ReadAsync(buffer);
                if ((ext == "gif" && AnimeGifEnable) || PureCSharpProcessing)
                {
                    gifimage = Image.Load(buffer, out format);
                    await JSRuntime.InvokeVoidAsync("setImgSrc", buffer, format.DefaultMimeType);
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
            image = new ImageData
            {
                Width = data[0],
                Height = data[1]
            };
            await SetImgContainterSize();
            MaxRatio = imgContainerWidth / imgRealW;
            Ratio = imgSize / 100;
            await OnLoad.InvokeAsync();
            await SizeChanged();
        }
        protected override async Task OnAfterRenderAsync(bool first)
        {
            await base.OnAfterRenderAsync(first);
            if (first)
            {
                setaction = () => SetImgContainterSize();
                mouseMoveAction = args =>
                {
                    OnSizeChanging(args);
                    OnResizeBackGroundImage(MouseToTouch(args));
                };
                mouseUpAction = args =>
                {
                    if (onTwoFingerResizing || isBacMoving)
                    {
                        isBacMoving = false;
                        onTwoFingerResizing = false;
                        InitBox();
                    }
                    OnSizeChangeEnd(args);
                };
                touchMoveAction = OnSizeChanging;
                touchEndAction = (args) =>
                {
                    outOfBox = true;
                    OnSizeChangeEnd(args);
                };
            }
        }
        #endregion

        #region JsInvokable methods
        [JSInvokable("OnTouchEnd")]
        public static void TouchEndCaller(MouseEventArgs args)
        {
            touchEndAction?.Invoke(args);
        }
        [JSInvokable("OnTouchMove")]
        public static void TouchMoveCaller(MouseEventArgs args)
        {
            touchMoveAction?.Invoke(args);
        }
        [JSInvokable("OnMouseUp")]
        public static void MouseUpCaller(MouseEventArgs args)
        {
            mouseUpAction?.Invoke(args);
        }
        [JSInvokable("OnMouseMove")]
        public static void MouseMoveCaller(MouseEventArgs args)
        {
            mouseMoveAction?.Invoke(args);
        }
        [JSInvokable("SetWidthHeight")]
        public static void SetWidthHeightCaller()
        {
            setaction?.Invoke();
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
            double i = 0d;

            if (widerThanContainer)
            {
                double containerWidth = imgContainerWidth * imgSize / 100;
                i = containerWidth / image.Width;
                deltaY = -(int)(imgContainerHeight / i - image.Height) / 2;
            }
            else
            {
                double containerHeight = imgContainerHeight * imgSize / 100;
                i = containerHeight / image.Height;
                deltaX = -(int)(imgContainerWidth / i - image.Width) / 2;
            }
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
            if (gifimage == null)
            {
                string s = await JSRuntime.InvokeAsync<string>("cropAsync", "oriimg", (int)((prevPosX - bacx) / i + deltaX), (int)((prevPosY - bacy) / i + deltaY),
                    (int)(cw), (int)(ch), 0, 0, (int)(cw * resizeProp), (int)(ch * resizeProp), "image/png");
                return new ImageCroppedResult(s);
            }
            else
            {
                Image img = gifimage.Clone(ctx =>
                {
                    ctx.Crop(new Rectangle((int)((prevPosX - bacx) / i + deltaX), (int)((prevPosY - bacy) / i + deltaY), (int)(cw), (int)(ch)));
                    if (resizeProp != 1d)
                    {
                        ctx.Resize(new Size((int)(cw * resizeProp), (int)(ch * resizeProp)));
                    }
                });
                return new ImageCroppedResult(img, format);
            }
        }
        #endregion


        #region private methods

        private double GetI()
        {
            double i = 0d;
            if (widerThanContainer)
            {
                double containerWidth = imgContainerWidth * imgSize / 100;
                i = containerWidth / image.Width;
            }
            else
            {
                double containerHeight = imgContainerHeight * imgSize / 100;
                i = containerHeight / image.Height;
            }
            return i;
        }

        private void SetCroppedImgStyle()
        {
            cropedImgStyle = $"clip: rect({prevPosY - layoutY}px, {prevPosX - layoutX + initCropWidth}px, {prevPosY - layoutY + initCropHeight}px, {prevPosX - layoutX}px);";
        }

        private void SetCropperStyle()
        {
            cropperStyle = $"top:{prevPosY}px;left:{prevPosX}px;cursor:move;height:{initCropHeight}px;width:{initCropWidth}px";
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
                outOfBox = true;
                return new MouseEventArgs();
            }
        }

        private void OnDragStart(MouseEventArgs args)
        {
            outOfBox = false;
            if (reSizing)
            {
                return;
            }
            SetCropperStyle();
            offsetX = args.ClientX;
            offsetY = args.ClientY;
            dragging = true;
        }

        private void OnDragging(MouseEventArgs args)
        {
            if (dragging && !reSizing)
            {
                double x = prevPosX - offsetX + args.ClientX;
                double y = prevPosY - offsetY + args.ClientY;
                if (y < minposY)
                {
                    outOfBox = true;
                    y = minposY;
                }
                if (x < minposX)
                {
                    outOfBox = true;
                    x = minposX;
                }
                if (y + initCropHeight > (minposY + imgh))
                {
                    outOfBox = true;
                    y = (minposY + imgh) - initCropHeight;
                }
                if (x + initCropWidth > (minposX + imgw))
                {
                    outOfBox = true;
                    x = (minposX + imgw) - initCropWidth;
                }
                unsavedX = x;
                unsavedY = y;
                cropperStyle = $"top:{y}px;left:{x}px;height:{initCropHeight}px;width:{initCropWidth}px";
                cropedImgStyle = $"clip: rect({y - layoutY}px, {x - layoutX + initCropWidth}px, {y - layoutY + initCropHeight}px, {x - layoutX}px);";
                base.StateHasChanged();
            }
        }

        private void OnDragEnd(MouseEventArgs args)
        {
            dragging = false;
            OnDragging(args);
            if (outOfBox)
            {
                prevPosX = unsavedX;
                prevPosY = unsavedY;
                return;
            }
            prevPosX = prevPosX - offsetX + args.ClientX;
            prevPosY = prevPosY - offsetY + args.ClientY;
        }

        private void OnResizeStart(MouseEventArgs args, MoveDir dir)
        {
            outOfBox = false;
            this.dir = dir;
            if (dragging)
            {
                return;
            }
            SetCropperStyle();
            offsetX = args.ClientX;
            offsetY = args.ClientY;
            if (IsCropLocked && !isBacMoving && !onTwoFingerResizing)
            {
                OnDragStart(args);
                return;
            }
            reSizing = true;
        }

        private void OnSizeChanging(MouseEventArgs args)
        {
            if (reSizing && !dragging)
            {
                double delta = args.ClientY - offsetY;
                double deltaX = args.ClientX - offsetX;
                double ytemp = prevPosY;
                double tempCropHeight = initCropHeight;
                double xtemp = prevPosX;
                double tempCropWidth = initCropWidth;
                switch (dir)
                {
                    case MoveDir.Up:
                        {
                            ytemp = prevPosY + delta;
                            tempCropHeight = initCropHeight - delta;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Down:
                        {
                            tempCropHeight = initCropHeight + delta;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Left:
                        {
                            xtemp = prevPosX + deltaX;
                            tempCropWidth = initCropWidth - deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropHeight = tempCropWidth * AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Right:
                        {
                            tempCropWidth = initCropWidth + deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropHeight = tempCropWidth * AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.UpLeft:
                        {
                            ytemp = prevPosY + delta;
                            tempCropHeight = initCropHeight - delta;
                            tempCropWidth = initCropWidth - deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                                deltaX = initCropWidth - tempCropWidth;
                            }
                            xtemp = prevPosX + deltaX;
                            break;
                        }
                    case MoveDir.UpRight:
                        {
                            ytemp = prevPosY + delta;
                            tempCropHeight = initCropHeight - delta;
                            tempCropWidth = initCropWidth + deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.DownLeft:
                        {
                            tempCropHeight = initCropHeight + delta;
                            tempCropWidth = initCropWidth - deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                                deltaX = initCropWidth - tempCropWidth;
                            }
                            xtemp = prevPosX + deltaX;
                            break;
                        }
                    case MoveDir.DownRight:
                        {
                            tempCropHeight = initCropHeight + delta;
                            tempCropWidth = initCropWidth + deltaX;
                            if (RequireAspectRatio)
                            {
                                tempCropWidth = tempCropHeight / AspectRatio;
                            }
                            break;
                        }
                    default:
                        break;
                }
                if (ytemp < minposY)
                {
                    outOfBox = true;
                    ytemp = minposY;
                }
                if (xtemp < minposX)
                {
                    outOfBox = true;
                    xtemp = minposX;
                }
                if (ytemp + tempCropHeight > (minposY + imgh))
                {
                    outOfBox = true;
                    tempCropHeight = (minposY + imgh) - ytemp;
                    if (RequireAspectRatio)
                    {
                        tempCropWidth = tempCropHeight / AspectRatio;
                    }
                }
                if (xtemp + tempCropWidth > (minposX + imgw))
                {
                    outOfBox = true;
                    tempCropWidth = (minposX + imgw) - xtemp;
                    if (RequireAspectRatio)
                    {
                        tempCropHeight = tempCropWidth / AspectRatio;
                    }
                }
                if (tempCropHeight < minval)
                {
                    tempCropHeight = minval;
                    ytemp = unsavedY;
                    xtemp = unsavedX;
                }
                if (tempCropWidth < minval)
                {
                    tempCropWidth = minval;
                    ytemp = unsavedY;
                    xtemp = unsavedX;
                }
                unsavedX = xtemp;
                unsavedY = ytemp;
                unsavedCropH = tempCropHeight;
                unsavedCropW = tempCropWidth;
                cropperStyle = $"top:{ytemp}px;left:{xtemp}px;height:{tempCropHeight}px;width:{tempCropWidth}px";
                cropedImgStyle = $"clip: rect({ytemp - layoutY}px, {xtemp - layoutX + tempCropWidth}px, {ytemp - layoutY + tempCropHeight}px, {xtemp - layoutX}px);";
            }
            OnDragging(args);
            base.StateHasChanged();
        }

        private void OnSizeChangeEnd(MouseEventArgs args)
        {
            if (reSizing)
            {
                reSizing = false;
                OnSizeChanging(args);
                double delta = args.ClientY - offsetY;
                double deltaX = args.ClientX - offsetX;
                if (outOfBox)
                {
                    initCropHeight = unsavedCropH;
                    initCropWidth = unsavedCropW;
                    prevPosY = unsavedY;
                    prevPosX = unsavedX;
                    return;
                }
                switch (dir)
                {
                    case MoveDir.Up:
                        {
                            prevPosY = prevPosY + delta;
                            initCropHeight -= delta;
                            if (RequireAspectRatio)
                            {
                                initCropWidth = initCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Down:
                        {
                            initCropHeight += delta;
                            if (RequireAspectRatio)
                            {
                                initCropWidth = initCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Left:
                        {
                            prevPosX += deltaX;
                            initCropWidth -= deltaX;
                            if (RequireAspectRatio)
                            {
                                initCropHeight = initCropWidth * AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.Right:
                        {
                            initCropWidth += deltaX;
                            if (RequireAspectRatio)
                            {
                                initCropHeight = initCropWidth * AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.UpLeft:
                        {
                            prevPosY = prevPosY + delta;
                            initCropHeight -= delta;
                            if (RequireAspectRatio)
                            {
                                double ori = initCropWidth;
                                initCropWidth = initCropHeight / AspectRatio;
                                deltaX = ori - initCropWidth;
                                prevPosX += deltaX;
                            }
                            else
                            {
                                prevPosX += deltaX;
                                initCropWidth -= deltaX;
                            }
                            break;
                        }
                    case MoveDir.UpRight:
                        {
                            prevPosY = prevPosY + delta;
                            initCropHeight -= delta;
                            initCropWidth += deltaX;
                            if (RequireAspectRatio)
                            {
                                initCropWidth = initCropHeight / AspectRatio;
                            }
                            break;
                        }
                    case MoveDir.DownLeft:
                        {
                            initCropHeight += delta;
                            if (RequireAspectRatio)
                            {
                                double ori = initCropWidth;
                                initCropWidth = initCropHeight / AspectRatio;
                                deltaX = ori - initCropWidth;
                                prevPosX += deltaX;
                            }
                            else
                            {
                                prevPosX += deltaX;
                                initCropWidth -= deltaX;
                            }
                            break;
                        }
                    case MoveDir.DownRight:
                        {
                            initCropHeight += delta;
                            initCropWidth += deltaX;
                            if (RequireAspectRatio)
                            {
                                initCropWidth = initCropHeight / AspectRatio;
                            }
                            break;
                        }
                    default:
                        break;
                }
                if (initCropHeight < minval)
                {
                    initCropHeight = minval;
                }
                if (initCropWidth < minval)
                {
                    initCropWidth = minval;
                }
            }
            if (dragging)
            {
                OnDragEnd(args);
            }
            SizeChanged();
        }

        private Task SizeChanged()
        {
            double i = GetI();
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

        private void ResizeBac(double i)
        {
            double temp = imgSize;
            if (imgSize * i < 100 && i < 1)
            {
                return;
            }
            if ((imgRealW * i > imgContainerWidth) && (imgRealH * i > imgContainerHeight) && i > 1)
            {
                return;
            }
            imgSize *= i;




            if (widerThanContainer)
            {
                minposX = 0;
                minposY = (imgContainerHeight - imgRealH) / 2;
            }
            else
            {
                minposY = 0;
                minposX = (imgContainerWidth - imgRealW) / 2;
            }
            minposY += bacy - (imgSize - temp) / imgSize * imgRealH / 2;
            minposX += bacx;
            if (prevPosX + initCropWidth > minposX + imgRealW || prevPosX < minposX
                || prevPosY + initCropHeight > minposY + imgRealH || prevPosY < minposY)
            {
                imgSize /= i;
            }
            else
            {
                bacy -= (i - 1) * imgRealH / 2;
                layoutY -= (i - 1) * imgRealH / 2;
                SetCroppedImgStyle();
            }
        }

        private void OnResizeBackGroundImage(TouchEventArgs args)
        {
            if (args.Touches.Length == 1)
            {
                if (!isBacMoving)
                {
                    return;
                }
                double dx = args.Touches[0].ClientX - prevBacX;
                double dy = args.Touches[0].ClientY - prevBacY;
                if (widerThanContainer)
                {
                    minposX = 0;
                    minposY = (imgContainerHeight - imgRealH) / 2;
                }
                else
                {
                    minposY = 0;
                    minposX = (imgContainerWidth - imgRealW) / 2;
                }
                minposY += bacy + dy;
                minposX += bacx + dx;
                bacx += dx;
                bacy += dy;
                layoutX += dx;
                layoutY += dy;
                if (prevPosX + initCropWidth > minposX + imgRealW || prevPosX < minposX)
                {
                    bacx -= dx;
                    layoutX -= dx;
                    minposX -= bacx;
                }
                if (prevPosY + initCropHeight > minposY + imgRealH || prevPosY < minposY)
                {
                    bacy -= dy;
                    layoutY -= dy;
                    minposY -= bacy;
                }
                SetCroppedImgStyle();
                prevBacX = args.Touches[0].ClientX;
                prevBacY = args.Touches[0].ClientY;
            }
            if (args.Touches.Length != 2)
            {
                return;
            }
            // two finger resize
            double distance = Math.Pow((args.Touches[0].ClientX - args.Touches[1].ClientX), 2) +
                Math.Pow((args.Touches[0].ClientY - args.Touches[1].ClientY), 2);
            if (onTwoFingerResizing)
            {
                double i = distance / prevTouchPointDistance;
                ResizeBac(i);
                Ratio = imgSize / 100;
            }
            else
            {
                onTwoFingerResizing = true;
            }
            prevTouchPointDistance = distance;

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
            imgContainerWidth = t[0];
            imgContainerHeight = t[1];
            InitStyles();
        }

        private void InitBox()
        {
            double prevPosX = 0d;
            double prevPosY = 0d;
            if (widerThanContainer)
            {
                prevPosY = (imgContainerHeight - imgRealH) / 2;
                prevPosX = 0;
            }
            else
            {
                prevPosX = Math.Abs(imgContainerWidth - imgRealW) / 2;
                prevPosY = 0;
            }
            prevPosY += bacy;
            prevPosX += bacx;
            minposX = prevPosX;
            minposY = prevPosY;
            imgw = imgRealW;
            imgh = imgRealH;
        }

        private void InitStyles()
        {
            double i = 0d;
            if (widerThanContainer)
            {
                i = imgContainerWidth / image.Width;
                prevPosY = (imgContainerHeight - imgRealH) / 2;
                prevPosX = 0;
            }
            else
            {
                i = imgContainerHeight / image.Height;
                prevPosX = Math.Abs(imgContainerWidth - imgRealW) / 2;
                prevPosY = 0;
            }
            prevPosY += bacy;
            prevPosX += bacx;
            minposX = prevPosX;
            minposY = prevPosY;
            imgw = imgRealW;
            imgh = imgRealH;
            if (initCropHeight > imgh)
            {
                initCropHeight = imgh;
            }
            if (initCropWidth > imgw)
            {
                initCropWidth = imgw;
            }

            SetCropperStyle();
            SetCroppedImgStyle();
            base.StateHasChanged();
        }
        #endregion
    }
}
