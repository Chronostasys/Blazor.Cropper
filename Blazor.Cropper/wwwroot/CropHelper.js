// This file is to show how a library package may provide JavaScript interop features
// wrapped in a .NET API
function addCropperEventListeners() {
    document.addEventListener('mousemove', (ev) => {
        try {
            DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseMove', serializeEvent(ev));
        } catch (error) {
        }
    })
    document.addEventListener('mouseup', (ev) => {
        try {
            DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseUp', serializeEvent(ev));
        } catch (error) {
        }
    })
    document.addEventListener('touchmove', (ev) => {
        try {
            DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchMove', {
                clientX: ev.touches[0].clientX,
                clientY: ev.touches[0].clientY
            });
        } catch (error) {

        }
    })
    document.addEventListener('touchend', (ev) => {
        try {
            DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchEnd');
        } catch (error) {

        }
    })
    window.addEventListener('resize', (ev) => {
        try {
            DotNet.invokeMethodAsync('Blazor.Cropper', 'SetWidthHeight');
        } catch (error) {
        }
    })
}
function getWidthHeight() {
    var a = [];
    var e = document.getElementById("blazor_cropper");
    a.push(e.clientWidth);
    a.push(e.clientHeight);
    return a;
}
function getOriImgSize() {
    var a = [];
    var e = document.getElementById("oriimg");
    a.push(e.naturalWidth);
    a.push(e.naturalHeight);
    return a;
}
async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format) {
    var blob = await new Promise(function (resolve) {
        var canvas = document.createElement('canvas');
        var img = document.getElementById(id);
        canvas.width = width;
        canvas.height = height;
        canvas.getContext('2d').drawImage(img, sx, sy, swidth, sheight, x, y, width, height);
        resolve(canvas.toDataURL(format));
    })
    blob = blob.substr(blob.indexOf(',') + 1)
    return blob;
}
function setImg(id) {
    var e = document.getElementById("blazor_cropper");
    e.parentElement.style.overflowX = 'hidden';
    var input = document.getElementById(id);
    var src = URL.createObjectURL(input.files[0]);
    document.getElementById('dimg').setAttribute('src', src);
    document.getElementById('oriimg').setAttribute('src', src);

}
function setImgSrc(bin, format) {
    var e = document.getElementById("blazor_cropper");
    e.parentElement.style.overflowX = 'hidden';
    src = 'data:' + format + ';base64,' + bin;
    document.getElementById('dimg').setAttribute('src', src);
    document.getElementById('oriimg').setAttribute('src', src);
}
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