new MutationObserver((mutations, observer) => {
    if (document.querySelector('#components-reconnect-modal h5 a')) {
        async function attemptReload() {
            location.reload();
        }
        observer.disconnect();
        attemptReload();
        setInterval(attemptReload, 10000);
    }
}).observe(document.body, { childList: true, subtree: true });

function BlazorDownloadFile(filename, contentType, data) {
    const file = new File([data], filename, { type: contentType });
    const exportUrl = URL.createObjectURL(file);
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = filename;
    a.target = "_self";
    a.click();
    URL.revokeObjectURL(exportUrl);
    a.remove();
}