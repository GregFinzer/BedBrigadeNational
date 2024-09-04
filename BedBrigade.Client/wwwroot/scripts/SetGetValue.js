window.SetGetValue = {

    CheckState: function () {
        return document.readyState;
    },
    
    SetInnerHtml: function (element, value) {    
            document.getElementById(element).innerHTML = value;
      },

    SetOuterHtml: function (element, value) {
            document.getElementById(element).outerHTML = value;
        },

        SetAttribute: function (element, attr, value) {
            var el = document.getElementById(element);
            if (el) {
                el.setAttribute(attr, value);
            }
        },

        GetInnerHtml: function (element) {
            return document.getElementById(element).innerHTML;
        },

        GetOuterHtml: function (element, value) {
            return document.getElementById(element).outerHTML;
        },

        GetAttribute: function (element, attr) {
            return document.getElementById(element).getAttribute(attr);
        }
}
