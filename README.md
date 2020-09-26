# Blazor.Cropper
A blazor library provide a component to crop image  
![](imgs/base.gif)=>  
![](imgs/1.gif) ![](imgs/2.gif) ![](imgs/3.gif)  

Live demo: [http://49.234.6.167/cropper](http://49.234.6.167/cropper)

It is:
- almost full c#
- fast
- mobile compatible
- lighweight
- support proportion
- **GIF crop support**(only for files smaller than 1mb)
- open source on [github](https://github.com/Chronostasys/Blazor.Cropper)  

If you find Blazor.Cropper helpful, you could **star this repo**, it's really important to me.  

For a long time, crop image in blazor bother me a lot. That's why I tried to implement a cropper in blazor.

## Usage
to use it, you should first paste following code into your index.html:  
```html
<script src="_content/Chronos.Blazor.Cropper/CropHelper.js"></script>
```
Then, you can install our [nuget pkg](https://www.nuget.org/packages/Chronos.Blazor.Cropper) and use it like follow:
```razor
@page "/cropper"
@inject IJSRuntime JSRuntime;

<h1>Cropper</h1>
<InputFile id="input1" OnChange="OnInputFileChange"></InputFile>
@if (parsing)
{
    <center>
        <h2>@prompt</h2>
    </center>
}
@if (!string.IsNullOrEmpty(imgUrl)&&!parsing)
{
    <center>
        <h2>Crop Result:</h2>
        <img src="@imgUrl" />
    </center>
}
@if (file != null)
{
    <div class="modal is-active">
        <div class="modal-background"></div>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Modal title</p>
                <button class="delete" aria-label="close" @onclick="()=>file=null"></button>
            </header>
            <section class="modal-card-body" style="overflow:hidden">
                <Cropper MaxCropedHeight="500" MaxCropedWidth="500" 
                    @ref="cropper"
                    Proportion="proportion==0?1:proportion" 
                    RequireProportion="bool.Parse(enableProportion)" 
                    InputId="input1" 
                    ImageFile="file"
                    @bind-Ratio="ratio"
                    OnLoad="OnCropperLoad"></Cropper>
            </section>
            <footer class="modal-card-foot">
                <button class="button is-success" @onclick="DoneCrop">Done</button>
                @if (cropper!=null)
                {
                    <input type="range" min="@(cropper.MinRatio*100)" max="@(cropper.MaxRatio*100)" value="@(ratio*100)" @oninput="OnRatioChange"/>
                }
                
            </footer>
        </div>
    </div>
}
<select @bind-value="enableProportion" @bind-value:event="onchange">
    <option value="true">Enable proportion</option>
    <option value="false">Disable proportion</option>
</select>
@if (bool.Parse(enableProportion))
{
    <input type="number" @bind-value="proportion" placeholder="proportion"/>
}
@code {
    Cropper cropper;
    IBrowserFile file;
    string imgUrl = "";
    Image image;
    string prompt = "Image cropped! Parsing to base64...";
    bool parsing = false;
    string enableProportion = "false";
    double proportion = 1d;
    double ratio = 1;
    void OnRatioChange(ChangeEventArgs args)
    {
        ratio = int.Parse(args.Value.ToString())/100.0;
    }
    protected override void OnInitialized()
    {
        
        base.OnInitialized();
    }
    
    void OnInputFileChange(InputFileChangeEventArgs args)
    {
        image?.Dispose();
        file = args.File;
    }
    void OnCropperLoad()
    {
        base.StateHasChanged();
    }
    async Task DoneCrop()
    {
        var args = await cropper.GetCropedResult();
        file = null;
        parsing = true;
        base.StateHasChanged();
        await Task.Delay(10);// a hack, otherwise prompt won't show
        image?.Dispose();
        await JSRuntime.InvokeVoidAsync("console.log", "converted!");
        imgUrl = args.Base64;
        parsing = false;
    }
}

```
For more details, see [the sample project](CropperSample).  
To build it, simply clone it and run it in visual studio. The running result should be like this:  
![](2020-09-26-12-29-30.png)  

