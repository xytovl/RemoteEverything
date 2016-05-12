$(function() {
	$(".window").draggable({ handle: "h1", stack: ".window" });
});

function generateUUID() {
	var d = new Date().getTime();
	var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c)
	{
		var r = (d + Math.random()*16)%16 | 0;
		d = Math.floor(d/16);
		return (c=='x' ? r : (r&0x3|0x8)).toString(16);
	});
	return uuid;
};

function WindowTemplate(re, data)
{
	if (data === undefined)
	{
		this.id = generateUUID();
		this.template_name = "New window";
		this.field_list = [];
		this.type_name = "";
	}
	else
	{
		this.id = data.id;
		this.template_name = data.template_name;
		this.field_list = data.field_list;
		this.type_name = data.type_name;
	}
	re.templates[this.id] = this;
}

WindowTemplate.prototype.contains = function(object, member)
{
	for(var i = 0; i < this.field_list.length; i++)
	{
		if (this.field_list[i].object == object && this.field_list[i].member == member)
			return true;
	}

	return false;
}

WindowTemplate.prototype.remove = function(object, member)
{
	for(var i = 0; i < this.field_list.length; i++)
	{
		if (this.field_list[i].object == object && this.field_list[i].member == member)
		{
			this.field_list.splice(i, 1);
			return;
		}
	}
}

WindowTemplate.prototype.append = function(object, member)
{
	if (!this.contains(object, member))
	{
		this.field_list.push({"object": object, "member": member});
		return true;
	}
	return false;
}

WindowTemplate.prototype.clear = function()
{
	this.field_list = [];
}

WindowTemplate.prototype.save = function()
{
	return {
		id: this.id,
		template_name: this.template_name,
		field_list: this.field_list,
		type_name: this.type_name
	};
}


function RemoteEverything(base_url)
{
	this.base_url = base_url;
	this.objects = {};
	this.window_list = [];

	this.load();

	window.addEventListener("storage", function(that)
	{
		return function(e)
		{
			if (e.key == "templates")
			{
				that.load(JSON.parse(e.newValue));

				that.updateAllTemplates();
			}
		};
	}(this));
}

RemoteEverything.prototype.load = function(data)
{
	this.templates = {};
	if (data === undefined)
	{
		try
		{
			data = JSON.parse(localStorage.getItem("templates"));
		}
		catch(e)
		{
			data = {};
		}
	}

	if (data === null)
		return;

	for(var i in data)
	{
		this.templates[i] = new WindowTemplate(this, data[i]);
	}
}

RemoteEverything.prototype.save = function()
{
	var templates = {};
	for(var i in this.templates)
	{
		templates[i] = this.templates[i].save();
	}
	localStorage.setItem("templates", JSON.stringify(templates));
}

RemoteEverything.prototype.updateAllTemplates = function()
{
	var deleted_windows = [];
	for(var i in this.window_list)
	{
		if (this.window_list[i].template_id in this.templates)
		{
			this.window_list[i].onTemplateUpdated(this.templates[this.window_list[i].template_id]);
		}
		else
		{
			deleted_windows.push(this.window_list[i]);
		}
	}

	for(var i in deleted_windows)
	{
		deleted_windows[i].close();
	}
}

RemoteEverything.prototype.refresh = function()
{
	var formData = "";
	for (var i in this.window_list)
	{
		var w = this.window_list[i];
		for (var f = 0 ; f < w.template.field_list.length ; f++)
		{
			if (formData.length != 0)
				formData += "&";
			var field = w.template.field_list[f];
			formData += w.logicalId + "=";
			formData += field.object + ";" + field.member;
		}
	}
	var xhr = new XMLHttpRequest();
	xhr.open("POST", this.base_url + "/list", true);
	xhr.onload = function(that)
	{
		return function(e)
		{
			var objectList = document.getElementById("object-list");
			var response = JSON.parse(xhr.responseText);
			var newdata = response.objects;

			for (var id in newdata)
			{
				if (that.objects[id] === undefined)
				{
					var listItem = document.createElement("li");
					var listItemLink = document.createElement("a");
					listItemLink.href = "";
					listItemLink.onclick = function(id)
						{
							return function()
							{
								ProtoWindow(that, id);
								return false;
							}
						}(id);

					if (response.objectNames[id] == undefined)
						listItemLink.textContent = id;
					else
						listItemLink.textContent = response.objectNames[id];
					listItem.appendChild(listItemLink);
					objectList.appendChild(listItem);
					that.objects[id] = listItem;
				}
			}

			var removed = [];
			for (var id in that.objects)
			{
				if (newdata[id] === undefined)
				{
					removed.push(id);
					objectList.removeChild(that.objects[id]);
				}
			}
			for (var i = 0 ; i < removed.length ; i++)
				delete that.objects[removed];

			for (var i = 0 ; i < that.window_list.length ; i++)
			{
				that.window_list[i].update(response);
			}

			that.refresh();
		};
	}(this);

	xhr.onerror = function(e)
	{
		$("#refresh-button").show();
		alert("error");
	};

	xhr.send(formData);
	$("#refresh-button").hide();
}

function ProtoWindow(re, logicalId)
{
	templates = [];
	for (var template in re.templates)
	{
		templates.push(re.templates[template]);
	}
	if (templates.length == 0)
	{
		var w = new Window(re, logicalId, new WindowTemplate(re));
		w.edit(true);
		return;
	}

	var element = document.createElement("div");
	element.classList.add("window", "proto-window");

	var h1 = document.createElement("h1");
	element.appendChild(h1);
	h1.textContent = "Select window settings";

	for (var i = 0; i < templates.length; i++)
	{
		div = document.createElement("div");
		div.className = "template-select";
		element.appendChild(div)
		button = document.createElement("a");
		button.textContent = templates[i].template_name;
		div.appendChild(button);
		button.onclick = function(template) {
			return function() {
				new Window(re, logicalId, template, element);
			}
		}(templates[i]);
		var del = document.createElement("a");
		del.textContent = "X";
		del.onclick = function(div, id)
		{
			return function() {
				if (confirm("Delete settings " + re.templates[id].template_name + "?"))
				{
					delete re.templates[id];
					element.removeChild(div);
					re.updateAllTemplates();
				}
				return false;
			}
		}(div, templates[i].id);
		div.appendChild(del)
	}

	var button = document.createElement("a");
	button.classList.add("new-template");
	button.textContent = "New";
	element.appendChild(button);
	button.onclick = function() {
		var w = new Window(re, logicalId, new WindowTemplate(re), element);
		w.edit(true);
	}

	$(element).draggable({ stack: ".window" });
	document.body.appendChild(element);
}

function Window(re, logicalId, template, div)
{
	var that = this;

	this.logicalId = logicalId;
	this.re = re;
	this.template = template;
	if (div == undefined)
	{
		this.element = document.createElement("div");
		document.body.appendChild(this.element);
	}
	else
	{
		this.element = div;
	}
	this.fields = {};
	this.template_id = template.id;

	this.element.className = "window data-window";

	re.window_list.push(this);

	this.element.innerHTML = '<h1>' +
		'<span class="window-title">'+
		'<span class="template-name"></span><input type="text" class="template-name"> (<span class="object-name"></span>)</span>' +
		'<span class="edit-button" title="edit">E</span>' +
		'<span class="close-button" title="close">X</span>' +
		'</h1>' +
		'<div class="window-content">' +
		'<table><tbody class="data-table"></tbody></table>' +
		'<div class="new-item"><select></select><button>+</button></div>' +
		'</div>';

	this.template_name_span = this.element.getElementsByClassName("template-name")[0];
	this.template_name = this.element.getElementsByClassName("template-name")[1];
	this.object_name = this.element.getElementsByClassName("object-name")[0];
	this.element.getElementsByClassName("close-button")[0].onclick = function() { that.close(); };
	this.element.getElementsByClassName("edit-button")[0].onclick = function() { that.edit(); };

	this.tbody = this.element.getElementsByTagName("tbody")[0];
	$(this.tbody).sortable({axis: "y"}).sortable("disable");
	$(this.tbody).disableSelection();

	$(this.element).draggable({ stack: ".window" });

	var addDiv = this.element.getElementsByClassName("new-item")[0];
	this.add_select = addDiv.getElementsByTagName("select")[0];
	addDiv.getElementsByTagName("button")[0].onclick = function(){
		var option = that.add_select.selectedOptions[0];
		if (template.append(option.dataset.type, option.dataset.field))
			that.onTemplateUpdated(template);
	};

	this.onTemplateUpdated(template);
}

Window.prototype.onTemplateUpdated = function(template)
{
	this.template = template;
	if (! this.element.classList.contains("window-editing"))
		this.template_name.value = template.template_name;
	this.template_name_span.textContent = template.template_name;

	this.fields = {};
	this.tbody.innerHTML = "";

	for (var i = 0 ; i < template.field_list.length ; i++)
	{
		var field = template.field_list[i];
		if (this.fields[field.object] == undefined)
		{
			this.fields[field.object] = {};
		}
		var dataField = new DataField(this, field);
		this.fields[field.object][field.member] = dataField;
		this.tbody.appendChild(dataField.tr);
	}
}

Window.prototype.update = function(response)
{
	var logicalObject = response.objects[this.logicalId];
	if (logicalObject == undefined)
		return;
	var objectName = response.objectNames[this.logicalId];
	if (objectName == undefined)
		this.object_name.textContent = this.logicalId;
	else
		this.object_name.textContent = objectName;

	for (var i = 0 ; i < this.template.field_list.length ; i++)
	{
		var field = this.template.field_list[i];
		var object = logicalObject[field.object];
		if (object == undefined)
			continue;
		var member = object[field.member];
		if (member == undefined)
			continue;
		if (this.fields[field.object] != undefined && this.fields[field.object][field.member] != undefined)
			this.fields[field.object][field.member].update(member);
	}

	if (this.add_select.children.length == 0)
	{
		for(var type in logicalObject)
		{
			for(var field in logicalObject[type])
			{
				var displayName = field;

				if (logicalObject[type][field].displayName !== undefined)
					displayName = logicalObject[type][field].displayName;

				var opt = document.createElement("option");
				opt.textContent = displayName;
				opt.dataset.type = type;
				opt.dataset.field = field;
				opt.dataset.displayName = displayName;
				this.add_select.appendChild(opt);
			}
		}
	}
}

Window.prototype.edit = function(focus)
{
	this.element.classList.toggle("window-editing");
	if (this.element.classList.contains("window-editing"))
	{
		$(this.tbody).sortable("enable");
		this.add_select.innerHTML = "";
		if (focus)
			this.template_name.focus();
	}
	else
	{
		$(this.tbody).sortable("disable");
		var dataFields = this.tbody.getElementsByTagName("tr");
		this.template.clear();
		for (var i = 0 ; i < dataFields.length ; i++)
		{
			this.template.append(dataFields[i].dataset.object, dataFields[i].dataset.member);
		}
		this.template.template_name = this.template_name.value;
		this.re.save();
		this.re.updateAllTemplates();
	}
}

Window.prototype.close = function()
{
	this.re.window_list.splice(this.re.window_list.indexOf(this), 1);
	this.element.parentNode.removeChild(this.element);
}

function DataField(parentWindow, field)
{
	this.tr = document.createElement("tr");
	this.tr.dataset.object = field.object;
	this.tr.dataset.member = field.member;
	this.delete_cell = document.createElement("td");
	this.name_cell = document.createElement("td");
	this.value_cell = document.createElement("td");
	this.value_data = document.createElement("span");
	this.value_unit = document.createElement("span");

	this.delete_cell.classList.add("delete-field");
	this.delete_cell.textContent = "D";
	this.delete_cell.onclick = function(tr)
	{
		return function()
		{
			parentWindow.template.remove(field.object, field.member);
			parentWindow.tbody.removeChild(tr);
			delete parentWindow.fields[field.object][field.member]
		};
	}(this.tr);

	this.value_cell.classList.add("data-field");
	this.value_data.classList.add("data-value");
	this.value_unit.classList.add("data-value-unit");

	this.tr.appendChild(this.delete_cell);
	this.tr.appendChild(this.name_cell);
	this.tr.appendChild(this.value_cell);
	this.value_cell.appendChild(this.value_data);
	this.value_cell.appendChild(this.value_unit);
}

DataField.SIprefix = function(value, unit)
{
	if (unit == "")
	{
		return [value.toPrecision(4), ""];
	}
	var mult = 0;

	while (Math.abs(value) >= 1000 && mult < 5)
	{
		value /= 1000;
		mult++;
	}

	while (Math.abs(value) < 1 && mult > -5)
	{
		value *= 1000;
		mult--;
	}

	var value_str = value.toFixed(2);
	switch(mult)
	{
		case -5:
			return [value_str, "f" + unit];
		case -4:
			return [value_str, "p" + unit];
		case -3:
			return [value_str, "n" + unit];
		case -2:
			return [value_str, "µ" + unit];
		case -1:
			return [value_str, "m" + unit];
		case 1:
			return [value_str, "k" + unit];
		case 2:
			return [value_str, "M" + unit];
		case 3:
			return [value_str, "G" + unit];
		case 4:
			return [value_str, "T" + unit];
		case 5:
			return [value_str, "P" + unit];
		default:
			return [value_str, unit];
	}
}

DataField.prototype.update = function(item)
{
	var value = item.value;

	this.name_cell.textContent = item.displayName;
	if (typeof(value) == "number")
	{
		var unit = item.unit || "";

		var value_str = DataField.SIprefix(value, unit);
		this.value_data.textContent = value_str[0];
		this.value_unit.textContent = value_str[1];
		this.value_unit.style.display = "";
	}
	else if (value !== undefined)
	{
		this.value_data.textContent = value;
		this.value_unit.style.display = "none";
	}
}


var ol = new RemoteEverything("http://" + window.location.hostname + ":8080");
ol.refresh();
