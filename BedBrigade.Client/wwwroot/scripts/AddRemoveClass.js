window.AddRemoveClass = {

    CheckState: function () {
        return document.readyState;
    },
    
    SetClass: function (element, value) {
        if (element == '') return;
        const elements = document.getElementsByClassName(value);
        if (elements.length > 0) {
            for (var i = 0; i < elements.length; i++) {
                elements[i].classList.remove(value);
            }
            el = document.getElementById(element);
            if (el == null) return;
            el.classList.add(value);
        }
    },

    RemoveClass: function (element, value) {
        if (element == '') return;
        el = document.getElementById(element);
        if (el == null) return;
        el.classList.remove(value);
    }
}
