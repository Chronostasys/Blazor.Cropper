# Blazor.Cropper

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/8184731f2b374089a64e53d24e1c09a7)](https://app.codacy.com/gh/Chronostasys/Blazor.Cropper?utm_source=github.com&utm_medium=referral&utm_content=Chronostasys/Blazor.Cropper&utm_campaign=Badge_Grade)
[![BCH compliance](https://bettercodehub.com/edge/badge/Chronostasys/Blazor.Cropper?branch=master)](https://bettercodehub.com/)
![GitHub](https://img.shields.io/github/license/Chronostasys/Blazor.Cropper?style=plastic)

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

## Quick Start
Only 4 steps to use Blazor.Cropper
### Step0. Add nuget  pkg
Install our nuget pkg at [nuget.org](https://www.nuget.org/packages/Chronos.Blazor.Cropper). 
Add namespace to `_import.razor`:  
```razor
@using Blazor.Cropper
```
### Step1. Add script referrence
Then, you should paste following code into your index.html:  
```html
<script src="_content/Chronos.Blazor.Cropper/CropHelper.js"></script>
```
### Step2. Add cropper
Just add cropper to your code. We recommend you to use it inside a modal card.  
**Note**: to use the cropper, you need to use a `<InputFile>` component to get a file source. 
You must provide a paramter named `InputId`, which's value is the same as the `id` attribute of the `<InputFile>` component.  
Example:
```razor
@* .... some code ...*@
<InputFile id="input1"></InputFile>
<Cropper InputId="input1" ></Cropper>
@* .... some code ...*@
```

### Step3. Get result
To get the crop result, you need to get the reference of the `Cropper`, then call the `Cropper.GetCropedResult()` method.  
Example:  
```razor
@* .... some code ...*@
<Cropper InputId="input1" @ref="cropper"></Cropper>
@* .... some code ...*@
@code{
    Cropper cropper;
    @* .... some code ...*@
    void GetCropResult()
    {
        var re = cropper.GetCropedResult();
        var buffer = re.GetBytes();
        var base64 = re.Base64;
    }
    @* .... some code ...*@
}
```


## Api referrence
We have detailed xml comments on Cropper's properties & methods, simply read it while use it!  
On the other hand, you can go to [the sample project](CropperSample) for usage examples.  
To build it, simply clone it and run it in visual studio. The running result should be like this:  
![](2020-09-26-12-29-30.png)  

