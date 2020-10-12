// This file is to show how a library package may provide JavaScript interop features
// wrapped in a .NET API
function getWidthHeight() {
    var a = [];
    var e = document.getElementById("blazor_cropper");
    a.push(e.clientWidth);
    a.push(e.clientHeight);
    return a;
}
function getOriImgSize(){
    var a = [];
    var e = document.getElementById("oriimg");
    a.push(e.naturalWidth);
    a.push(e.naturalHeight);
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
    var e = document.getElementById("blazor_cropper");
    e.parentElement.style.overflowX='hidden';
    var input = document.getElementById(id);
    var src = URL.createObjectURL(input.files[0]);
    document.getElementById('dimg').setAttribute('src',src);
    document.getElementById('oriimg').setAttribute('src',src);
    
}
window.addEventListener('resize', (ev) => {
    DotNet.invokeMethodAsync('Blazor.Cropper', 'SetWidthHeight');
})
var serializeEvent = function (e) {
    if (e) {
        var o = {
            altKey: e.altKey,
            button: e.button,
            buttons: e.buttons,
            clientX: e.clientX,
            clientY: e.clientY,
            ctrlKey: e.ctrlKey,
            metaKey: e.metaKey,
            movementX: e.movementX,
            movementY: e.movementY,
            offsetX: e.offsetX,
            offsetY: e.offsetY,
            pageX: e.pageX,
            pageY: e.pageY,
            screenX: e.screenX,
            screenY: e.screenY,
            shiftKey: e.shiftKey
        };
        return o;
    }
};
document.addEventListener('mousemove', (ev)=>{
    DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseMove', serializeEvent(ev));
})
document.addEventListener('mouseup', (ev)=>{
    DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseUp', serializeEvent(ev));
})
document.addEventListener('touchmove', (ev)=>{
    DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchMove', {
        clientX:ev.touches[0].clientX,
        clientY:ev.touches[0].clientY
    });
})
document.addEventListener('touchend', (ev)=>{
    DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchEnd');
})