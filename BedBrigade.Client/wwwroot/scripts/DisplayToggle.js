window.DisplayToggle = {

    CheckState: function () {
        return document.readyState;
    },
    
    Toggle: function (element) {
        if (element == '') return;
        const x = document.getElementById(element);
        if (x == null) return;
        if (x.style.display === "none") {
            x.style.display = "block";
        }
        else {
            x.style.display = "none";
        }
    }
}
