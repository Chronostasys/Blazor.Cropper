function setSrc(bin, type) {
    console.log(bin,type)
    document.getElementById('my-img').src = URL.createObjectURL(
        new Blob([bin], { type: 'image/png' })
    );
}