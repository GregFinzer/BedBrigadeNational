﻿window.AddRemoveClass = {

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
            el.classList.add(value);
        }
    }
}