let websocket;

$().ready(function () {
    websocket = null;
    connect();
});

function connect() {
    var url = "";

    if (window.location.href.indexOf("https") != -1)
        url = "wss" + window.location.href.substring(5);
    else
        url = "ws" + window.location.href.substring(4);
        
    webSocket = new WebSocket(url);

    webSocket.onopen = function () {
        console.log("connected");
        webSocket.send("register;0;" + groupId + ";downinfo");
        console.log("sent register");
    };



    webSocket.onmessage = function (evt) {
        var resp;
        try {
            resp = JSON.parse(evt.data);
        } catch (err) {
            console.log(evt);
            console.log(evt.data);
            console.log(err);
            return;
        }

        console.log(resp);
        
        switch (resp.type) {
            case "error":
                setIcon(resp.id, "warning_amber");
                setColor(resp.id, "red");
                setProgress(resp.id, -1);
                setVis(resp.id, "pause", false);
                setVis(resp.id, "stop", false);
                $("li[data-id=" + resp.id + "] span.errmsg").html(resp.message);
                break;
            case "reset":
                setIcon(resp.id, "mif-hour-glass fg-black");
                setColor(resp.id, "gray darken-1");
                setProgress(resp.id, 0);
                setVis(resp.id, "pause", true);
                setVis(resp.id, "stop", false);
                setVis(resp.id, "delete", false);
                break;
            case "check":
                setIcon(resp.id, "mif-spinner3 fg-dargGray ani-spin");
                setPbCol(resp.id, "yellow");
                setPbVal(resp.id, 100);
                break;
            case "extract":
                setIconGroup(resp.group, "folder_zip");
                setColorGroup(resp.group, "purple lighten-2");
                setProgress(resp.id, resp.perc);
                setVis(resp.id, "pause", false);
                setVis(resp.id, "stop", true);
                setVis(resp.id, "delete", false);
                break;
            case "downloaded":
                setIcon(resp.id, "file_download_done");
                setColor(resp.id, "orange darken-2");
                setProgress(resp.id, -1);
                setVis(resp.id, "pause", true);
                setVis(resp.id, "stop", false);
                setVis(resp.id, "delete", true);
                break;
            case "extracted":
                setIconGroup(resp.group, "folder");
                setColorGroup(resp.group, "purple darken-2");
                setProgressGroup(resp.group, -1);
                setVisGroup(resp.group, "pause", true);
                setVisGroup(resp.group, "stop", false);
                setVisGroup(resp.group, "delete", false);
                break;
            case "moving":
                setIconGroup(resp.group, "copy_all");
                setColorGroup(resp.group, "brown lighten-1");
                setProgressGroup(resp.group, -1);
                setVis(resp.id, "pause", false);
                setVis(resp.id, "stop", false);
                setVis(resp.id, "delete", false);
                break;
            case "fin":
                setIconGroup(resp.group, "task_alt");
                setColorGroup(resp.group, "green");
                setProgressGroup(resp.group, -1);
                setVisGroup(resp.group, "pause", false);
                setVisGroup(resp.group, "stop", false);
                setVisGroup(resp.group, "delete", false);
                break;
            case "info":
                setIcon(resp.id, "downloading");
                setColor(resp.id, "orange lighten-2");
                setProgress(resp.id, resp.perc);
                setVis(resp.id, "pause", false);
                setVis(resp.id, "stop", true);
                setVis(resp.id, "delete", false);
                break;
        }
    };
}

function setVis(id, action, isVisible) {
    if(isVisible)
        $("li[data-id=" + id + "] a[data-action=" + action + "]").removeClass("hide");
    else
        $("li[data-id=" + id + "] a[data-action=" + action + "]").addClass("hide");
}

function setVisGroup(id, action, isVisible) {
    if(isVisible)
        $("li[data-group=" + id + "] a[data-action=" + action + "]").removeClass("hide");
    else
        $("li[data-group=" + id + "] a[data-action=" + action + "]").addClass("hide");
}

function setIcon(id, icon) {
    $("li[data-id=" + id + "] > i").html(icon);
}

function setIconGroup(id, icon) {
    $("li[data-group=" + id + "] > i").html(icon);
}

function setColor(id, color) {
    $("li[data-id=" + id + "] > i").attr("class", "material-icons circle " + color);
}

function setColorGroup(id, color) {
    $("li[data-group=" + id + "] > i").attr("class", "material-icons circle " + color);
}

function setProgress(id, value) {
    $("li[data-id=" + id + "] .progress").css("display", (value >= 0 ? "block" : "none"));
    if(value == -1) value = 0;
    $("li[data-id=" + id + "] .determinate").css("width", value + "%");
}

function setProgressGroup(id, value) {
    $("li[data-group=" + id + "] .progress").css("display", (value >= 0 ? "block" : "none"));
    if(value == -1) value = 0;
    $("li[data-group=" + id + "] .determinate").css("width", value + "%");
}