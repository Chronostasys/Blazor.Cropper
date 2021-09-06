function setSrc(bin) {
    console.log(bin)
    document.getElementById('my-img').src = URL.createObjectURL(
        new Blob([bin], { type: 'image/png' })
    );
}
