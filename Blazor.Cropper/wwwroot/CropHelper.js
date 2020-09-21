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
function setImg(id) {
    var input = document.getElementById(id);
    var src = URL.createObjectURL(input.files[0]);
    document.getElementById('dimg').setAttribute('src',src);
    document.getElementById('oriimg').setAttribute('src',src);
    
}
window.addEventListener('resize', (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'SetWidth');
    } catch (e) {
        console.error(e);
    }
})