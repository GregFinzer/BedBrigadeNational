window.DisplayToggle = {

    CheckState: function () {
        return document.readyState;
    },
    
    Show: function (element) {
        if (element == '') return;
        var ele = document.getElementById(element);
        if (ele == null) return;
        ele.style.display = "block";
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
        ele.style.display = "none";
    }
}


