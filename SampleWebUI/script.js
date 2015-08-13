$(function() {
    $(".window").draggable({ handle: "h1", stack: ".window" });
});

function RemoteEverything(base_url)
{
    this.base_url = base_url;
    this.window_list = {};
    this.data = {};
}

RemoteEverything.prototype.refresh = function()
{
    var xhr = new XMLHttpRequest();
    xhr.open("GET", this.base_url + "/list", true);
    xhr.onload = function(that)
    {
        return function(e)
        {
            var objectList = document.getElementById("object-list");
            var listItems = objectList.getElementsByTagName("li");

            newdata = JSON.parse(xhr.responseText).objects;

            for(var i = 0; i < listItems.length; )
            {
                if (newdata[listItems[i].dataset.logicalId] === undefined)
                    objectList.removeChild(listItems[i]);
                else
                    i++;
            }

            var listWindows = document.getElementsByClassName("data-window");
            for(var i = 0; i < listWindows.length; )
            {
                if (newdata[listWindows[i].dataset.logicalId] === undefined)
                    listWindows[i].parentNode.removeChild(listWindows[i]);
                else
                    i++;
            }
            
            for(var id in newdata)
            {
                if (that.data[id] === undefined)
                {
                    var o = newdata[id];
                    var listItem = document.createElement("li");
                    listItem.dataset.logicalId = id;
                    var listItemLink = document.createElement("a");
                    listItemLink.href = "";
                    listItemLink.onclick = function(id)
                                        {
                                            return function()
                                            {
                                                that.createWindow(id);
                                                return false;
                                            }
                                        }(id);
                    
                    var listItemText = document.createTextNode(id);
                    listItemLink.appendChild(listItemText);
                    listItem.appendChild(listItemLink);
                    objectList.appendChild(listItem);
                }
            }

            that.data = newdata;

            var values = document.getElementsByClassName("data-value");
            for(var i = 0; i < values.length; i++)
            {
                var id = values[i].dataset.logicalId;
                var type = values[i].dataset.type;
                var field = values[i].dataset.field;
                $(values[i]).empty();
                if (that.data[id][type][field].value !== undefined)
                    values[i].appendChild(document.createTextNode(that.data[id][type][field].value));
            }

            var tables = document.getElementsByClassName("data-table");
            for(var i = 0; i < tables.length; i++)
            {
                
            }
            
            that.refresh();
        };
    }(this);

    xhr.onerror = function(e)
    {
        $("#refresh-button").show();
        alert("error");
    };

    xhr.send(null);
    $("#refresh-button").hide();
}

RemoteEverything.prototype.createWindow = function(id)
{
    var w = new Window(document.body, "Data for " + id);
    w.element.dataset.logicalId = id;
    
    
    for(var type in this.data[id])
    {
        for(var field in this.data[id][type])
        {
            var tr = document.createElement("tr");
            var td1 = document.createElement("td");
            var td2 = document.createElement("td");
            var td3 = document.createElement("td");

            td1.classList.add("delete-field");
            td1.appendChild(document.createTextNode("D"));
            td1.onclick = function(tbody, tr, w)
            {
                return function()
                {
                    tbody.removeChild(tr);
                    w.save();
                };
            }(w.tbody, tr, w);
            
            if (this.data[id][type][field].displayName === undefined)
                td2.appendChild(document.createTextNode(field));
            else
                td2.appendChild(document.createTextNode(this.data[id][type][field].displayName));
            
            td3.classList.add("data-value");
            td3.dataset.logicalId = id;
            td3.dataset.type = type;
            td3.dataset.field = field;

            tr.appendChild(td1);
            tr.appendChild(td2);
            tr.appendChild(td3);
            w.tbody.appendChild(tr);
        }
    }
}

function Window(parent, title)
{
    var that = this;
    
    this.title = title;
    this.element = document.createElement("div");
    this.element.classList.add("window", "data-window");
    
    this.element.innerHTML = '<h1>' +
        '<span class="window-title"></span>' +
        '<span class="close-button">X</span>' +
        '<span class="edit-button">E</span>' +
        '</h1>' +
        '<div class="window-content"><table><tbody class="data-table"></tbody></table></div>';

    this.element.getElementsByClassName("window-title")[0].appendChild(document.createTextNode(title));
    this.element.getElementsByClassName("close-button")[0].onclick = function() { that.close(); };
    this.element.getElementsByClassName("edit-button")[0].onclick = function() { that.edit(); };
    parent.appendChild(this.element);
    
    this.content = this.element.getElementsByClassName("window-content")[0];
    this.tbody = this.element.getElementsByTagName("tbody")[0];
    $(this.tbody).sortable({axis: "y"}).sortable("disable");
    $(this.tbody).disableSelection();
    
    $(this.element).draggable({ handle: "h1", stack: ".window" });
    $(this.element).on("sortupdate", function() { that.save(); });
}

Window.prototype.edit = function()
{
    this.element.classList.toggle("window-editing");
    if (this.element.classList.contains("window-editing"))
    {
        $(this.tbody).sortable("enable");
    }
    else
    {
        $(this.tbody).sortable("disable");
    }
}

Window.prototype.save = function()
{
    console.log("save");
    // TODO: enregistrer dans localStorage
}

Window.prototype.close = function()
{
    this.element.parentNode.removeChild(this.element);
}

var ol = new RemoteEverything("http://" + window.location.hostname + ":8080");
ol.refresh();
