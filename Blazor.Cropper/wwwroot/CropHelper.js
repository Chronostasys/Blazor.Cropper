// This file is to show how a library package may provide JavaScript interop features
// wrapped in a .NET API

function getWidthHeight() {
    var a = [];
    var e = document.getElementById("blazor_cropper");
    a.push(e.clientWidth);
    a.push(e.clientHeight);
    return a;
}
async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format) {
    var blob =  await new Promise(function (resolve) {
        var canvas = document.createElement('canvas');
        var img = document.getElementById(id);
        canvas.width = width;
        canvas.height = height;
        canvas.getContext('2d').drawImage(img, sx, sy, swidth, sheight, x, y, width, height);
        resolve(canvas.toDataURL(format));
    })
    
    return blob;
}
window.addEventListener('resize', (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'SetWidth');
    } catch (e) {
        console.error(e);
    }
})