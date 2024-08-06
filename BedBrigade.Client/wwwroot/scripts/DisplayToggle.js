window.DisplayToggle = {

    CheckState: function () {
        return document.readyState;
    },
    
    Toggle: function (element) {
        if (element == '') return;
        var ele = document.getElementById(element);
        if (ele == null) return;
        if (ele.style.display === "none") {
            ele.style.display = "block";
        }
        else {
            ele.style.display = "none";
        }
    },

    Show: function (element) {
        if (element == '') return;
        var ele = document.getElementById(element);
        if (ele == null) return;
        if (ele.style.display === "none") {
            ele.style.display = "block";
        }
    },

    ShowByClass: function (className) {
        if (className == '') return;
        var elements = document.getElementsByClassName(className);
        for (var i = 0; i < elements.length; i++) {
            elements[i].style.display = "block";
        }
    },

    HideByClass: function (className) {
        if (className == '') return;
        var elements = document.getElementsByClassName(className);
        for (var i = 0; i < elements.length; i++) {
            elements[i].style.display = "block";
        }
    },

    Hide: function (element) {
        if (element == '') return;
        var ele = document.getElementById(element);
        if (ele == null) return;
        if (ele.style.display === "block") {
            ele.style.display = "none";
        }
    },

    SetDisplay: function (element, value) {
        if (element == '' || value == '') return;
        var ele = document.getElementById(element);
        if (ele == null || (value != "none" && value != "block") ) return;
        ele.style.display = value;
    }
}


