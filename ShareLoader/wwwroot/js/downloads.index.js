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
        //webSocket.send("register;0;0;downinfo");
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
    }

    checkGroupsInfo();
}

function checkGroupsInfo()
{
    $.getJSON(window.location.origin + "/Downloads/GetGroupsInfo", (data) => {
        console.log(data);
        $.each(data, (index, ele) => {
            $("li[data-id=" + ele.id + "] span.countD").html(ele.downloaded);
            $("li[data-id=" + ele.id + "] span.countE").html(ele.extracted);
            $("li[data-id=" + ele.id + "] span.countF").html(ele.finished);
            $("li[data-id=" + ele.id + "] span.countX").html(ele.error);
        });
    });

    setTimeout(checkGroupsInfo, 10000);
}

function startAddFile()
{
    $("#InputAddFile").click();
}

function changedAddFile() {
    $("#FormAddFile").submit();
}