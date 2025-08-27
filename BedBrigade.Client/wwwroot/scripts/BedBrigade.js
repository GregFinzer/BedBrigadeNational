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

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

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

window.BedBrigadeUtil = {
    SelectMaskedText: function (elementId, position) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
            element.setSelectionRange(position, position);
        }
    },
    InitializeJarallax: function (imgPosition = 'top') {
        jarallax(document.querySelectorAll('.jarallax'), {
            speed: 0.2,
            imgPosition: imgPosition
        });
    },
    GetBrowserLocale: function () {
        return (navigator.languages && navigator.languages.length) ? navigator.languages[0] : navigator.userLanguage || navigator.language || navigator.browserLanguage || 'en-US';
    },
    ScrollPastImages: function () {
        window.scrollTo({
            top: 500,
            behavior: 'instant'
        });
    },
    //From here:  https://stackoverflow.com/questions/5007530/how-do-i-scroll-to-an-element-using-javascript
    ScrollToElementId: function (elementId, scrollAdditional = 0) {
        const element = document.getElementById(elementId);
        if (element) {
            window.scrollTo({
                top: 0,
                behavior: 'instant'
            });

            element.style.visibility = 'visible';
            element.style.display = 'block';
            element.setAttribute('tabindex', '-1');
            element.focus();
            element.removeAttribute('tabindex');

            if (scrollAdditional) {
                window.scrollBy({
                    top: scrollAdditional,
                    behavior: 'instant'
                });
            }
        } else {
            console.error(`Element with id ${elementId} not found`);
        }
    },
    ScrollToBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },
    playNotification: function () {        
        const audio = new Audio('/sounds/Notification.mp3');
        audio.play();
    },
    runCarousel: function (intervalMilliseconds) {
        console.log('runCarousel');
        document.querySelectorAll("[id*='carousel']").forEach(function (carouselElement) {
            console.log('Carousel ID:  ' + carouselElement);

            let carousel = new bootstrap.Carousel(carouselElement, {
                interval: intervalMilliseconds, 
                ride: 'carousel',
                pause: false // Ensure it never stops even when out of view
            });

            // Override Bootstrap's default behavior that pauses the carousel when not visible
            let observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (!entry.isIntersecting) {
                        carousel.cycle(); // Continue cycling even when out of view
                    }
                });
            }, { threshold: 0 });

            observer.observe(carouselElement);
        });
    }
}