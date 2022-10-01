let stype = "";
let season = 0;
let imdbid = "";
let currentPage = 0;
let totalPage = 0;
let currentCheckIndex = 0;
let items = [];

$(document).ready(function () {
    checkSearch();
    checkSeason();
    checkLinks();

    $("#submit").click(e => {
        checkSubmit();
    });
});

$("#inSearch").keyup(function () {
    let query = $("#inSearch").val();
    let patt = new RegExp("tt([0-9]{3,7})+");
    let res = patt.test(query);

    if (res) {
        imdbid = query;
        getSeasons(query, true);
    } else {
        imdbid = "";
        currentPage = 1;
        search();
        $("input[name=NameToSort]").attr("value", query.replace(" ", "."));
    }
});

$("#Type").change(function () {
    setName("none");
    checkSearch();
});

function checkSubmit() {
    let itemCount = 0;
    $("#LinkGrid li.collection-item").each((index, target) => {
        let ele = JSON.parse($(target).attr("data-json"));
        console.log(ele);
        if($("input", target).is(":checked"))
        {
            Object.keys(ele).forEach(key => {
                $("#linkForm").append('<input type="hidden" name="Links[' + itemCount + '].' + key + '" value="' + ele[key] + '" />')
            });
            itemCount++;
        }
    });
    $("#linkForm").submit();
}

function checkLinks() {
    let perc = Math.round((100 / links.length) * currentCheckIndex);
    $("#progressbar").css("width", perc + "%");
    if(currentCheckIndex == links.length)
    {
        items.sort((a,b) => (a.name > b.name) ? 1 : ((b.name > a.name) ? -1 : 0))
        console.log(items);
        groupLinks();
        return;
    }
    let item = links[currentCheckIndex];
    getInfoDDL(item);
    currentCheckIndex++;
}

function groupLinks() {
    let groups = {};
    let offlineCounter = 0;
    let pattern1 = /\.part[0-9]+\.rar/;
    Object.keys(items).forEach(key => {
        let name = items[key].name;
        if(!items[key].isOnline)
        {
            offlineCounter++;
            $("#LinkGridOffline").css("display", "block")
            $("#LinkGridOffline").append('<li class="collection-item">' + items[key].downloader + ' - <a href="' + items[key].url + '">' + items[key].id + '</a></li>')
            return;
        }
        if(pattern1.test(name)) {
            let group = name.substring(0, name.search(pattern1));
            console.log(name + " -> " + group);
            if(groups[group] == undefined)
                groups[group] = {};
            groups[group][key] = items[key];
        } else {
            let group = name;
            if(groups[group] == undefined)
                groups[group] = {};
            groups[group][key] = items[key];
        }
    });

    let idCounter = 0;
    Object.keys(groups).forEach(key => {
        let group = groups[key];
        $("#LinkGrid").append('<li data-group="' + idCounter + '" class="collection-header"><label><input type="checkbox" checked="checked" /><span /></label><h5>' + key + '</h5></li>');

        Object.keys(group).forEach(key2 => {
            let ele = group[key2];
            ele.groupId = idCounter;
            $("#LinkGrid").append('<li data-json=\'' + JSON.stringify(ele) + '\' data-group="' + idCounter + '" class="collection-item avatar"><i class="material-icons circle ' + (ele.isOnline ? 'green' : 'red') + '">insert_drive_file</i><span class="title">' + ele.name + '</span><p>' + ele.sizeRead + '</p><div class="secondary-content"><label><input type="checkbox" ' + (ele.isOnline ? 'checked="checked"' : '') + ' /><span /></label></div></li>')
        });
        idCounter++;
    });

    $("#LinkGrid li.collection-header input").change((a) => {
        console.log("Changed Checkbox");
        console.log(a.currentTarget);
        let value = $(a.currentTarget).is(":checked");
        console.log(value);
        let group = $(a.currentTarget).parent().parent().attr("data-group");
        console.log(group);
        $("li[data-group=" + group + "].collection-item input").prop('checked', value);
    });

    $("#LinkGrid li.collection-item input").change((a) => {
        let group = $(a.currentTarget).parent().parent().parent().attr("data-group");
        let value = $(a.currentTarget).is(":checked");
        let totalCount = 0;
        let checkedCount = 0;

        $("li[data-group=" + group + "].collection-item input").each((index, ele) => {
            totalCount++;
            if($(ele).is(":checked"))
                checkedCount++;
        });
        
        $("li[data-group=" + group + "].collection-header input").prop('checked', totalCount == checkedCount);
    });

    if(offlineCounter > 0)
        alert("Es sind " + offlineCounter + " Dateien offline!");
    $("#submit").removeClass("disabled");
}

function getInfoDDL(url) {
    let domain = url.substring(8);
    domain = domain.substring(0, domain.indexOf('/'));
    domain = domain.replace('.', "%2E");
    let id = url.substring(7 + domain.length);
    if(id.indexOf('/') != -1)
        id = id.substring(0, id.indexOf('/'));
        
    console.log(id);
    $.getJSON(window.location.origin + "/Downloads/GetItemInfo/?url=" + encodeURIComponent(url), function (data) {
        let ele = $("div.row[data-id=" + id + "]");
        $("div.state", ele).html(data.isOnline ? "Online" : "Offline");
        $("div.name", ele).html(data.name);

        let expos = [ "B", "KB", "MB", "GB" ];
        let expo = 0;
        let size = data.size;
        while(size >= 1024)
        {
            expo++;
            size = size / 1024;
        }
        $("div.size", ele).html(Math.round(size) + " " + expos[expo]);

        items.push({
            "id": id,
            "isOnline": data.isOnline,
            "name": data.name,
            "size": data.size,
            "sizeRead": Math.round(size) + " " + expos[expo],
            "downloader": data.downloader,
            "url": data.url
        });
        checkLinks();
    });
}

function checkSeason() {
    let query = $("#Name").val();
    let patt = new RegExp("S([0-9]{1,2})+");
    let res = patt.exec(query);

    if (res == null)
        return;

    season = res[1];
}

function checkSearch() {
    $("#inSeason").html("");
    switch ($("#Type").val()) {
        case "1":
            stype = "movie";
            console.log("Film");
            $("#inSearch").removeAttr('disabled');
            $("#inSeasonD").css("display", "none");
            $("#NameToSort").removeAttr('disabled');
            $("#NameToSort").attr('data-validate', 'required');
            break;
        case "2":
            stype = "series";
            console.log("Serie");
            $("#inSearch").removeAttr('disabled');
            $("#inSeasonD").css("display", "block");
            $("#NameToSort").removeAttr('disabled');
            $("#NameToSort").attr('data-validate', 'required');
            break;
        case "3":
            stype = "";
            console.log("Anderes");
            $("#inSearch").attr('disabled', 'disabled');
            $("#inSeasonD").css("display", "none");
            $("#NameToSort").attr('disabled', 'disabled');
            $("#NameToSort").removeAttr('data-validate');
            break;
        default:
            $("#inSearch").attr('disabled', 'disabled');
            $("#inSeasonD").css("display", "none");
            $("#NameToSort").attr('disabled', 'disabled');
            $("#NameToSort").removeAttr('data-validate');
            break;
    }
    currentPage = 1;
    search();
}

function searchBack() {
    if (currentPage <= 1)
        return;

    currentPage -= 1;
    search();
}

function searchNext() {
    if(currentPage >= totalPage) 
        return;
    currentPage += 1;
    search();
}

function search() {
    $("#searchBar").css("visibility", "visible");
    let query = $("#inSearch").val();
    $.getJSON(window.location.origin + "/Downloads/GetSearchResults?query=" + query + "&page=" + currentPage + "&type=" + stype, function (data) {
        console.log(data);
        $("#searchResults").html("");
        if (data.Response == "False") {
            currentPage = 0;
            totalPage = 0;
            $("#totalPage").html(totalPage);
            $("#currentPage").html(currentPage);
            $("#searchResults").html(data.Error);
            $("#searchBar").css("visibility", "hidden");
            return;
        }

        $("#currentPage").html(currentPage);
        totalPage = Math.ceil(data.totalResults / 10);
        $("#totalPage").html(totalPage);

        for (i = 0; i < data.Search.length; i++) {
            var item = data.Search[i];
            let poster = item.Poster;
            if (poster == "N/A")
                poster = "http://www.citypages.com/img/movie-placeholder.gif";
            $("#searchResults").append('<img onerror="javascript:imgFail(this);" title="' + item.Title + '" alt="' + item.Title + '" src="' + poster + '" data-imdbid="' + item.imdbID + '" data-sort="' + item.Title + '" data-year="' + item.Year + '">');
        }
        addClick();
        $("#searchBar").css("visibility", "hidden");
    });
}

function imgFail(ele) {
    $(ele).attr("src", "http://www.citypages.com/img/movie-placeholder.gif");
}

function addClick() {
    $("#searchResults img").click(function () {
        $("#searchResults img").removeClass("selected");
        var t = $(this);
        t.addClass("selected")
        imdbid = t.attr("data-imdbid");

        if ($("#Type").val() == "2") {
            getSeasons(t.attr("data-imdbid"));
            setName("none");
        } else {
            setName(t.attr("data-sort"), t.attr("data-year"));
        }
    });
}

function getSeasons(imdbid, addName = false) {
    $.getJSON(window.location.origin + "/Downloads/GetSeasons/" + imdbid, function (data) {
        if (data.Response == "False") {
            alert("Imdb ID nicht gefunden");
            setName("none");
            $("#inSeasonD").html('<select id="inSeason"></select>');
            $('select').formSelect();
            return;
        }

        if (addName) {
            setName(data.Title, data.Year);
            $("#searchResults").html(data.Title);
        }

        if (data.totalSeasons == undefined) return;

        let selectele = '<select id="inSeason">';

        selectele += "<option value=\"select\" disabled=\"disabled\">Bitte Staffel auswählen</option>";
        selectele += "<option value=\"dynamisch\">Staffelpaket</option>";
        for (let i = 1; i <= data.totalSeasons; i++) {
            let iteration = i;
            if (iteration < 10)
                iteration = "0" + i;
            if (iteration == season) {
                selectele += "<option value=\"" + iteration + "\" selected>Staffel " + iteration + "</option>";
            }
            else
                selectele += "<option value=\"" + iteration + "\">Staffel " + iteration + "</option>";
        }

        selectele += '</select>';
        $("#inSeasonD").html(selectele);
        $('select').formSelect();
        setName();
        $("#inSeason").change(function () {
            if ($("#inSeason").val() != "select")
                setName();
            else
                setname("none");
        });
    });
}

function setName(name = "", year = 0) {
    if (name == "none") {
        $("input[name=NameToSort]").attr("value", "");
        return;
    }

    let sort = name;
    console.log(sort);
    if (sort == "")
        sort = $("#searchResults img.selected").attr("data-sort");
    console.log(sort);

    if ($("#Type").val() == "2") {
        sort = sort + "/" + "Staffel " + $("#inSeason").val();
    } else {
        if (year == 0)
            sort = sort + " " + $("#searchResults img.selected").attr("data-year");
        else
            sort = sort + "(" + year + ")";

        if (imdbid != "")
            sort = sort + "[imdbid=" + imdbid + "]";

        console.log("id: " + imdbid);

        while (sort.indexOf(" ") != -1) {
            sort = sort.replace(" ", ".");
        }
    }
    
    
    
    $("input[name=NameToSort]").attr("value", sort);
}