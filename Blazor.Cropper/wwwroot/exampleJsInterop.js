// This file is to show how a library package may provide JavaScript interop features
// wrapped in a .NET API

function getWidth() {
    console.log(document.getElementById("blazor_cropper").clientWidth)
    return document.getElementById("blazor_cropper").clientWidth;
}
