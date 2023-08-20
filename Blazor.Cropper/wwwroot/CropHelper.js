// blazor.cropper code MIT license

const onMousemove = (ev, dotNetHelper) => {
    try {
        dotNetHelper.invokeMethodAsync('OnMouseMove', serializeEvent(ev));
    } catch (error) {
    }
}
const onMouseup = (ev, dotNetHelper) => {
    try {
        dotNetHelper.invokeMethodAsync('OnMouseUp');
    } catch (error) {
    }
}
const onTouchmove = (ev, dotNetHelper) => {
    try {
        dotNetHelper.invokeMethodAsync('OnTouchMove', {
            clientX: ev.touches[0].clientX,
            clientY: ev.touches[0].clientY
        });
    } catch (error) {

    }
}
const onTouchend = (ev, dotNetHelper) => {
    try {
        dotNetHelper.invokeMethodAsync('OnTouchEnd');
    } catch (error) {

    }
}
const onResize = (ev, dotNetHelper) => {
    try {
        dotNetHelper.invokeMethodAsync('SetWidthHeight');
    } catch (error) {
    }
}
function addCropperEventListeners(dotNetHelper) {
    document.addEventListener('mousemove', (ev) => onMousemove(ev, dotNetHelper))
    document.addEventListener('mouseup', (ev) => onMouseup(ev, dotNetHelper))
    document.addEventListener('touchmove', (ev) => onTouchmove(ev, dotNetHelper))
    document.addEventListener('touchend', (ev) => onTouchend(ev, dotNetHelper))
    window.addEventListener('resize', (ev) => onResize(ev, dotNetHelper))
}
function rmCropperEventListeners() {
    // document.removeEventListener('mousemove', onMousemove)
    // document.removeEventListener('mouseup', onMouseup)
    // document.removeEventListener('touchmove', onTouchmove)
    // document.removeEventListener('touchend', onTouchend)
    // window.removeEventListener('resize', onResize)
}


function getWidthHeight(e) {
    var a = [];
    a.push(e.clientWidth);
    a.push(e.clientHeight);
    return a;
}
function getOriImgSize(e) {
    var a = [];
    a.push(e.naturalWidth);
    a.push(e.naturalHeight);
    return a;
}
let version = 5;
function setVersion(ver) {
    version = ver;
}
async function cropAsync(img, sx, sy, swidth, sheight, x, y, width, height, format, quality) {
    var canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d')
    ctx.drawImage(img, sx, sy, swidth, sheight, x, y, width, height);
    if (version === 6) {
        return await new Promise((rs, rv) => {
            canvas.toBlob(async b => {
                const bin = new Uint8Array(await new Response(b).arrayBuffer());
                rs(bin)
            }, format, quality / 100);
        })
    }
    else {
        const s = canvas.toDataURL(format, quality / 100);
        return s.substr(s.indexOf(',') + 1)
    }
}
function setImg(id, cropper, dimg, oriimg) {
    var e = cropper;
    e.parentElement.style.overflowX = 'hidden';
    var input = document.getElementById(id);
    var src = URL.createObjectURL(input.files[0]);
    dimg.setAttribute('src', src);
    oriimg.setAttribute('src', src);

}
function setImgSrc(bin, format, cropper, dimg, oriimg) {
    if (bin.constructor === Uint8Array) {
        dimg.src = URL.createObjectURL(
            new Blob([bin], { type: format })
        );
        oriimg.src = URL.createObjectURL(
            new Blob([bin], { type: format })
        );
        return
    }
    var e = cropper;
    e.parentElement.style.overflowX = 'hidden';
    src = 'data:' + format + ';base64,' + bin;
    dimg.setAttribute('src', src);
    oriimg.setAttribute('src', src);
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

function setSrc(bin, id, type) {
    document.getElementById(id).src = URL.createObjectURL(
        new Blob([bin], { type: type })
    );
}
