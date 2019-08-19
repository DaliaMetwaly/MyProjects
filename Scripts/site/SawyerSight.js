var selectedNodes = new Array();
var visibleUnchecked = new Array();
initialPopulation = true;

$(document).ready(function () {
    $(window).keydown(function (event) {
        if (event.keyCode == 13) {
            if ($('#respectEnter').length == 0) {
                event.preventDefault();
                return false;
            }
        }
    });
});

$(document).ready(function () {
    $('.k-grid-header').addClass('table-dark-header');

});

function ProcessLoadedContext() {
    if ($("#hiddenActionLoggerChecked").val().length > 0) {
        selectedNodes = $("#hiddenActionLoggerChecked").val().split(',');
    }
    if ($("#hiddenActionLoggerUnChecked").val().length > 0) {
        visibleUnchecked = $("#hiddenActionLoggerUnChecked").val().split(',');
    }
}

function LogAction(myID, propagate) {
    myElementID = "cbx" + myID;
    var parentCheckbox = document.getElementById(myElementID);
    var isChecked = parentCheckbox.checked;
    if (isChecked) {
        if (selectedNodes.includes(myID)) {
            return;
        }
        else {
            var indexUn = visibleUnchecked.indexOf(myID);
            if (indexUn > -1) {
                visibleUnchecked.splice(indexUn, 1);
            }
            selectedNodes.push(myID);
        }
    }
    else {
        if (selectedNodes.includes(myID)) {
            var index = selectedNodes.indexOf(myID);
            if (index > -1) {
                selectedNodes.splice(index, 1);
            }
            if (propagate == true) {
                visibleUnchecked.push(myID);
            }
            else {
                var indexUnchecked = visibleUnchecked.indexOf(myID);
                if (indexUnchecked > -1) {
                    visibleUnchecked.splice(index, 1);
                }
            }
        }
    }
    $("#hiddenActionLoggerChecked").val(selectedNodes.join());
    $("#hiddenActionLoggerUnChecked").val(visibleUnchecked.join());
}

function RefreshSelections() {
    var datasource = $("#treelist").data("kendoTreeList").dataSource;
    var treelist = datasource.view();
    var selectAllActive = $('#selectAllActive').val();
    var selectAllInactive = $('#selectAllInactive').val();

    refreshMe(datasource, treelist, false, selectAllActive, selectAllInactive);

    function refreshMe(datasource, nodes, parentChecked, selectAllActive, selectAllInactive) {
        for (var i = 0; i < nodes.length; i++) {
            var id = nodes[i].Value.Id;
            var chk = document.getElementById('cbx' + id).getAttribute("activeflag");
            if (visibleUnchecked.includes(id)) {
                document.getElementById('cbx' + id).checked = false;
                refreshMe(datasource, datasource.childNodes(nodes[i]), false, selectAllActive, selectAllInactive);
            }
            else if (selectedNodes.includes(id)) {
                document.getElementById('cbx' + id).checked = true;
                refreshMe(datasource, datasource.childNodes(nodes[i]), true, selectAllActive, selectAllInactive);
            }
            else if (selectAllActive == 'true' && chk == 'Y') {
                document.getElementById('cbx' + id).checked = true;
                refreshMe(datasource, datasource.childNodes(nodes[i]), true, selectAllActive, selectAllInactive);
            }
            else if (selectAllActive == 'true' && chk == 'N') {
                document.getElementById('cbx' + id).checked = false;
                refreshMe(datasource, datasource.childNodes(nodes[i]), false, selectAllActive, selectAllInactive);
            }
            else if (selectAllInactive == 'true' && chk == 'N') {
                document.getElementById('cbx' + id).checked = true;
                refreshMe(datasource, datasource.childNodes(nodes[i]), true, selectAllActive, selectAllInactive);
            }
            else if (selectAllInactive == 'true' && chk == 'Y') {
                document.getElementById('cbx' + id).checked = false;
                refreshMe(datasource, datasource.childNodes(nodes[i]), false, selectAllActive, selectAllInactive);
            }
            else {
                document.getElementById('cbx' + id).checked = parentChecked;
                refreshMe(datasource, datasource.childNodes(nodes[i]), parentChecked, selectAllActive, selectAllInactive);
            }
        }
    }
}
function SelectMyChildren(myID, propagate) {
    var rootElement;
    myElementID = "cbx" + myID;
    var parentCheckbox = document.getElementById(myElementID);
    var state = parentCheckbox.checked;
    var datasource = $("#treelist").data("kendoTreeList").dataSource;
    var treelist = datasource.view();
    for (var i = 0; i < treelist.length; i++) {
        if (myID == treelist[i].Value.Id) {
            LogAction(myID, true);
            update(datasource, datasource.childNodes(treelist[i]), state, propagate);
        }
    }

    function update(datasource, nodes, state, propagate) {
        for (var i = 0; i < nodes.length; i++) {

            if (visibleUnchecked.includes(nodes[i].Value.Id)) {
                document.getElementById('cbx' + nodes[i].Value.Id).checked = false;
            }
            else {
                var chk = document.getElementById('cbx' + nodes[i].Value.Id).getAttribute("activeflag");
                var selectAllActive = $('#selectAllActive').val();
                var selectAllInactive = $('#selectAllInactive').val();
                if (initialPopulation == false) {
                    document.getElementById('cbx' + nodes[i].Value.Id).checked = state;
                    if (selectedNodes.includes(nodes[i].Value.Id) && !propagate) {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                    }
                    else if (!propagate) {
                        if (selectAllActive == 'true' && chk == 'Y') {
                            document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                        }
                        if (selectAllActive == 'true' && chk == 'N') {
                            document.getElementById('cbx' + nodes[i].Value.Id).checked = false;
                        }
                        if (selectAllInactive == 'true' && chk == 'Y') {
                            document.getElementById('cbx' + nodes[i].Value.Id).checked = false;
                        }
                        if (selectAllInactive == 'true' && chk == 'N') {
                            document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                        }
                        if (selectedNodes.includes(nodes[i].Value.Id)) {
                            document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                        }
                    }
                }
                else {
                    if (selectAllActive == 'true' && chk == 'Y') {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                    }
                    if (selectAllActive == 'true' && chk == 'N') {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = false;
                    }
                    if (selectAllInactive == 'true' && chk == 'Y') {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = false;
                    }
                    if (selectAllInactive == 'true' && chk == 'N') {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                    }
                    if (selectedNodes.includes(nodes[i].Value.Id)) {
                        document.getElementById('cbx' + nodes[i].Value.Id).checked = true;
                    }
                }
            }

            LogAction(nodes[i].Value.Id, false);
            update(datasource, datasource.childNodes(nodes[i]), state, propagate);
        }
    }
}

function CheckByValueAndType(value, type) {
    var selector = '[name="' + type + '"';
    $(selector).prop("checked", false);
    selectedNodes = new Array();
    visibleUnchecked = new Array();
    $('#selectAllActive').val('false');
    $('#selectAllInactive').val('false');
    switch (value) {
        case 'All':
            $(selector).prop("checked", true);
            $('#selectAllActive').val('true');
            $('#selectAllInactive').val('true');
            visibleUnchecked = new Array();
            break;
        case 'Active':
            $('[activeFlag="Y"').prop("checked", true);
            $('#selectAllActive').val('true');
            RemoveAllActiveFromUnselected();
            break;
        case 'Inactive':
            $('[activeFlag="N"').prop("checked", true);
            $('#selectAllInactive').val('true');
            RemoveAllInactiveFromUnselected();
            break;
        case 'Clear':
            $(selector).prop("checked", false);
            selectedNodes = new Array();
            visibleUnchecked = new Array();
            break;
    }
    $("#hiddenActionLoggerChecked").val(selectedNodes.join());
    $("#hiddenActionLoggerUnChecked").val(visibleUnchecked.join());
}

var latestparent = "0";

function onTreeDataBound(arg) {
    if (latestparent != "0") {
        SelectMyChildren(latestparent, false);
        RefreshSelections();
    }
    if (initialPopulation) {
        RefreshSelections();
        initialPopulation = false;
    }
}

function onProjectsTreeDataBound(arg) {
    onTreeDataBound(arg);
    var selectedLevel = $("#selectedProjectLevel").val();
    $.get("GetProjectMaxLevel", function (data) {
        var counter = data;
        var startProject = 'XXXX';
        var body = $("#projectLevelTableBody");
        body.html('');
        for (var i = 1; i <= counter; i++) {
            var row = '';
            if (selectedLevel == i) {
                row = '<tr><td></td><td>' + i + '</td><td>' + startProject + '</td><td><input type="radio" name="projectLevel" value="' + i + '" required checked></td><td></td></tr>';
            }
            else {
                row = '<tr><td></td><td>' + i + '</td><td>' + startProject + '</td><td><input type="radio" name="projectLevel" value="' + i + '" required></td><td></td></tr>';
            }

            body.html(body.html() + row);
            startProject = startProject + '.XXXX';
        }
    });
}
function onTreeExpand(arg) {
    latestparent = arg.model.Value.Id;
}

function RemoveAllActiveFromUnselected() {
    var elementsToRemove = new Array();
    visibleUnchecked.forEach(function (element) {
        console.log(element);
        myElementID = "cbx" + element;
        var chk = document.getElementById(myElementID).getAttribute("activeflag");
        if (chk == 'Y') {
            elementsToRemove.push(element);
        }
    });
    elementsToRemove.forEach(function (element) {
        var index = visibleUnchecked.indexOf(element);
        if (index > -1) {
            visibleUnchecked.splice(index, 1);
        }
    });
}

function RemoveAllInactiveFromUnselected() {
    var elementsToRemove = new Array();
    visibleUnchecked.forEach(function (element) {
        console.log(element);
        myElementID = "cbx" + element;
        var chk = document.getElementById(myElementID).getAttribute("activeflag");
        if (chk == 'N') {
            elementsToRemove.push(element);
        }
    });
    elementsToRemove.forEach(function (element) {
        var index = visibleUnchecked.indexOf(element);
        if (index > -1) {
            visibleUnchecked.splice(index, 1);
        }
    });
}

function onRevenueDataBound(arg) {
    var view = this.dataSource.view();
    this.items().each(function (index, row) {
        kendo.bind(row, view[index]);
    });
}

function revenuePropagate(e) {
    var node = e.items && e.items[0];
    var propagatedField = "checked";

    // only propagate changes to the desired field
    if (!node || e.field != propagatedField) {
        return;
    }

    this.unbind("change", revenuePropagate);

    function update(dataSource, nodes, field, state) {
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].set(field, state);

            update(dataSource, dataSource.childNodes(nodes[i]), field, state);
        }
    }

    update(this, this.childNodes(node), propagatedField, node[propagatedField]);

    this.bind("change", revenuePropagate);
}

function AddRevenueCategory(e) {
    e.preventDefault();
    var selAccounts = new Array();
    var selRows = new Array();
    var revenue = $("#revenueName").val();
    if (revenue == "") {
        alert('Please put a Revenue Name');
        return;
    }
    var tree = $("#treelist").data("kendoTreeList");
    var treelist = tree.dataSource.view();
    for (var i = 0; i < treelist.length; i++) {
        var myElementID = "cbx" + treelist[i].Value.Id;
        var chk = document.getElementById(myElementID);
        var chkAccount = document.getElementById(myElementID).getAttribute("actualAccount");
        if (chk != null && chk.checked) {
            selRows.push(i);
            if (chkAccount == "1") {
                selAccounts.push(treelist[i].Value.Id);
            }
        }
    }
    if (selRows.length == 0) {
        alert('You need to select at least 1 Revenue Account');
        return;
    }

    var selRevenue = $("#selectedRevenue").val();
    selRevenue = selRevenue + ';' + revenue + '|' + selAccounts.join(',');
    $("#selectedRevenue").val(selRevenue);

    selRows.forEach(function (entry) {
        tree.removeRow(treelist[entry]);
    });

    $("#revenueList").append($("<li>").text(revenue));
    $("#revenueName").val('');
}

function AddMandatoryRevenueCategory(e) {
    e.preventDefault();
    var selAccounts = new Array();
    var selRows = new Array();
    var revenue = $("#revenueName").val();
    if (revenue == "") {
        alert('Please put a Revenue Name');
        return;
    }
    var tree = $("#treelist").data("kendoTreeList");
    var treelist = tree.dataSource.view();
    for (var i = 0; i < treelist.length; i++) {
        var myElementID = "cbx" + treelist[i].Value.Id;
        var chk = document.getElementById(myElementID);
        var chkAccount = document.getElementById(myElementID).getAttribute("actualAccount");
        if (chk != null && chk.checked) {
            selRows.push(i);
            if (chkAccount == "1") {
                selAccounts.push(treelist[i].Value.Id);
            }
        }
    }
    if (selRows.length == 0) {
        alert('You need to select at least 1 Revenue Account');
        return;
    }

    var selRevenue = $("#selectedRevenue").val();
    selRevenue = selRevenue + ';' + revenue + '|' + selAccounts.join(',');
    $("#selectedRevenue").val(selRevenue);

    $("#revenueForm").submit();
}

var directCostCategoryAdded = false;
function AddCostsCategory(e) {
    e.preventDefault();
    var selAccounts = new Array();
    var selRows = new Array();
    var cost = $("#costsName").val();
    if (cost == "") {
        alert('Please put a Costs Name');
        return;
    }
    var tree = $("#treelist").data("kendoTreeList");
    var treelist = tree.dataSource.view();
    for (var i = 0; i < treelist.length; i++) {
        var myElementID = "cbx" + treelist[i].Value.Id;
        var chk = document.getElementById(myElementID);
        var chkAccount = document.getElementById(myElementID).getAttribute("actualAccount");
        if (chk != null && chk.checked) {
            selRows.push(i);
            if (chkAccount == "1") {
                selAccounts.push(treelist[i].Value.Id);
            }
        }
    }
    if (selRows.length == 0) {
        alert('You need to select at least 1 Costs Account');
        return;
    }

    var selCosts = $("#selectedCosts").val();
    selCosts = selCosts + ';' + cost + '|' + selAccounts.join(',');
    $("#selectedCosts").val(selCosts);

    selRows.forEach(function (entry) {
        tree.removeRow(treelist[entry]);
    });
    directCostCategoryAdded = true;
    $("#costsList").append($("<li>").text(cost));
    $("#costsName").val('');
}

function AddLaborCostsCategory(e) {
    e.preventDefault();
    var selAccounts = new Array();
    var selRows = new Array();
    var cost = $("#costsName").val();
    if (cost == "") {
        alert('Please put a Direct Labor Name');
        return;
    }
    var tree = $("#treelist").data("kendoTreeList");
    var treelist = tree.dataSource.view();
    for (var i = 0; i < treelist.length; i++) {
        var myElementID = "cbx" + treelist[i].Value.Id;
        var chk = document.getElementById(myElementID);
        var chkAccount = document.getElementById(myElementID).getAttribute("actualAccount");
        if (chk != null && chk.checked) {
            selRows.push(i);
            if (chkAccount == "1") {
                selAccounts.push(treelist[i].Value.Id);
            }
        }
    }
    if (selRows.length == 0) {
        alert('You need to select at least 1 Costs Account');
        return;
    }

    var selCosts = $("#costsName").val();
    selCosts = selCosts + ';' + cost + '|' + selAccounts.join(',');
    $("#selectedCosts").val(selCosts);

    $("#laborCostsForm").submit();
}

function AddSubcontractorCostsCategory(e) {
    e.preventDefault();
    var selAccounts = new Array();
    var selRows = new Array();
    var cost = $("#costsName").val();
    if (cost == "") {
        alert('Please put a Subcontractor Labor Name');
        return;
    }
    var tree = $("#treelist").data("kendoTreeList");
    var treelist = tree.dataSource.view();
    for (var i = 0; i < treelist.length; i++) {
        var myElementID = "cbx" + treelist[i].Value.Id;
        var chk = document.getElementById(myElementID);
        var chkAccount = document.getElementById(myElementID).getAttribute("actualAccount");
        if (chk != null && chk.checked) {
            selRows.push(i);
            if (chkAccount == "1") {
                selAccounts.push(treelist[i].Value.Id);
            }
        }
    }
    if (selRows.length == 0) {
        alert('You need to select at least 1 Costs Account');
        return;
    }

    var selCosts = $("#costsName").val();
    selCosts = selCosts + ';' + cost + '|' + selAccounts.join(',');
    $("#selectedCosts").val(selCosts);

    $("#subcontratorCostsForm").submit();
}

function SubmitCostsCategory(e) {
    e.preventDefault();
    if (directCostCategoryAdded == false) {
        alert('At least one Other Direct Cost category is mandatory');
        return;
    }
    else {
        $("#directCostsForm").submit();
    }

}

function PreselectFiscalYears() {
    if (initialPopulation) {
        var view = $("#treelist").data("kendoTreeList").dataSource.view();
        this.items().each(function (index, row) {
            kendo.bind(row, view[index]);
        });
        var selectedFY = new Array();
        if ($("#hiddenSelectedFY").val().length > 0) {
            selectedFY = $("#hiddenSelectedFY").val().split(',');
        }
        var datasource = $("#treelist").data("kendoTreeList").dataSource;
        var treelist = datasource.view();
        updateFY(datasource, treelist);

        function updateFY(datasource, nodes) {
            for (var i = 0; i < nodes.length; i++) {
                if (selectedFY.includes(nodes[i].Value.YearCode)) {
                    document.getElementById('cbx' + nodes[i].Value.YearCode).checked = true;
                }
                updateFY(datasource, datasource.childNodes(nodes[i]));
            }
        }

        initialPopulation = false;
    }
}

function SubmitDemographics(e) {
    e.preventDefault();
    var checkedItem = $(".demographicsCheck").filter(':checked').length;
    if (checkedItem == 0) {
        alert('At least one Demographics category is mandatory');
        return;
    }
    else {
        $("#demographicsForm").submit();
    }

}

