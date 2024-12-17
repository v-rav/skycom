window.fileManagementJsFunctions = {
    formUpload: async function (formId) {
        var form = document.getElementById(formId);

        var res = await fetch(location.origin + "/api/v1/file", {
            method: 'POST',
            body: new FormData(form)
        });
        var status = await res.status
        var body = await res.text();
        if (res.ok) {
            form.reset();
        }
        return { 'StatusCode':status, 'Body':body };
    },

    getFileSize: function (fileId) {
        var fileList = document.getElementById(fileId).files;
        return fileList[0].size;
    },

    clearFile: function (formId, fileId) {
        var fileList = document.getElementById(fileId).files;
        fileList[0].value = "";

        var form = document.getElementById(formId);
        form.reset();
    }
};