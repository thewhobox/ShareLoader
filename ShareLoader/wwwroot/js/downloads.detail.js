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
        
        /*
            case States.Finished:
                return "green";

            case States.Downloaded:
                return "orange darken-2";

            default:
            case States.Waiting:
                return "gray darken-1";

            case States.Downloading:
                return "orange lighten-2";

            case States.Error:
                return "red";

            case States.Extracting:
                return "purple lighten-2";

            case States.Extracted:
                return "purple darken-2";

            case States.Moving:
                return "brown lighten-1";*/

        switch (resp.type) {
            case "error":
                setIcon(resp.id, "warning_amber");
                setColor(resp.id, "red");
                setProgress(resp.id, 100);
                //setText(resp.id, "Error");
                break;
            case "reset":
                setIcon(resp.id, "mif-hour-glass fg-black");
                setColor(resp.id, "gray darken-1");
                setProgress(resp.id, 0);
                //setText(resp.id, "Zurückgesetzt");
                break;
            case "check":
                setIcon(resp.id, "mif-spinner3 fg-dargGray ani-spin");
                setPbCol(resp.id, "yellow");
                setPbVal(resp.id, 100);
                setText(resp.id, "Check");
                break;
            case "extract":
                setIconGroup(resp.group, "folder_zip");
                setColorGroup(resp.group, "purple lighten-2");
                setProgress(resp.id, resp.perc);
                //setText(resp.id, "Extracting");
                break;
            case "downloaded":
                setIcon(resp.id, "file_download_done");
                setColor(resp.id, "orange darken-2");
                setProgress(resp.id, -1);
                //setText(resp.id, "Downloaded");
                break;
            case "extracted":
                setIconGroup(resp.group, "folder");
                setColorGroup(resp.group, "purple darken-2");
                setProgressGroup(resp.group, -1);
                //setTexts(resp.id, "Extracted");
                break;
            case "moving":
                setIconGroup(resp.group, "copy_all");
                setColorGroup(resp.group, "brown lighten-1");
                setProgressGroup(resp.group, -1);
                //setTexts(resp.id, "Moving");
                break;
            case "fin":
                setIconGroup(resp.group, "task_alt");
                setColorGroup(resp.group, "green");
                setProgressGroup(resp.group, -1);
                //setTexts(resp.id, "");
                break;
            case "info":
                setIcon(resp.id, "downloading");
                setColor(resp.id, "orange lighten-2");
                setProgress(resp.id, resp.perc);
                //setText(resp.id).html(resp.speed);
                break;
        }
    };
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