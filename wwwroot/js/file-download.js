// Save a file produced on the server (Blazor DotNetStreamReference) without a separate endpoint.
window.fileDownload = {
    fromStream: async function (fileName, streamRef) {
        const buffer = await streamRef.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer]));
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName ?? "";
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    }
};
