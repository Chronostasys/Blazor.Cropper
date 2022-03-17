// blazor.cropper code MIT license

const onMousemove = (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseMove', serializeEvent(ev));
    } catch (error) {
    }
}
const onMouseup = (ev) => {
    console.log("up")
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'OnMouseUp', serializeEvent(ev));
    } catch (error) {
    }
}
const onTouchmove = (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchMove', {
            clientX: ev.touches[0].clientX,
            clientY: ev.touches[0].clientY
        });
    } catch (error) {

    }
}
const onTouchend = (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'OnTouchEnd', {
            clientX: ev.touches[0].clientX,
            clientY: ev.touches[0].clientY
        });
    } catch (error) {

    }
}
const onResize = (ev) => {
    try {
        DotNet.invokeMethodAsync('Blazor.Cropper', 'SetWidthHeight');
    } catch (error) {
    }
}
function addCropperEventListeners() {
    document.addEventListener('mousemove', onMousemove)
    document.addEventListener('mouseup', onMouseup)
    document.addEventListener('touchmove', onTouchmove)
    document.addEventListener('touchend', onTouchend)
    window.addEventListener('resize', onResize)
}
function rmCropperEventListeners() {
    document.removeEventListener('mousemove', onMousemove)
    document.removeEventListener('mouseup', onMouseup)
    document.removeEventListener('touchmove', onTouchmove)
    document.removeEventListener('touchend', onTouchend)
    window.removeEventListener('resize', onResize)
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
let version = 5;
function setVersion(ver) {
    version = ver;
}
async function cropAsync(id, sx, sy, swidth, sheight, x, y, width, height, format) {
    var canvas = document.createElement('canvas');
    var img = document.getElementById(id);
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d')
    ctx.drawImage(img, sx, sy, swidth, sheight, x, y, width, height);
    if (version === 6) {
        return await new Promise((rs, rv) => {
            canvas.toBlob(async b => {
                const bin = new Uint8Array(await new Response(b).arrayBuffer());
                rs(bin)
            });
        })
    }
    else {
        const s = canvas.toDataURL(format);
        return s.substr(s.indexOf(',') + 1)
    }
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
    if (bin.constructor === Uint8Array) {
        document.getElementById('dimg').src = URL.createObjectURL(
            new Blob([bin], { type: format })
        );
        document.getElementById('oriimg').src = URL.createObjectURL(
            new Blob([bin], { type: format })
        );
        return
    }
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

function setSrc(bin,id) {
    document.getElementById(id).src = URL.createObjectURL(
        new Blob([bin], { type: 'image/png' })
    );
}