window.projectAtmaca = window.projectAtmaca || {};

window.projectAtmaca.downloadFile = (fileName, contentType, content) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
};
