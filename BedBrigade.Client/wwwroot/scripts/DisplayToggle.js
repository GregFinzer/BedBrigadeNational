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

    SetDisplay: function (element, value) {
        if (element == '' || value == '') return;
        var ele = document.getElementById(element);
        if (ele == null || (value != "none" && value != "block") ) return;
        ele.style.display = value;
    }
}


