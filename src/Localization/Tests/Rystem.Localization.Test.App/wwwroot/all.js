window.getBrowserLanguage = () => {
    return navigator.language || navigator.userLanguage;
};


function setLanguageCookie(language) {
    document.cookie = "lang=" + language + "; path=/";
}

function getLanguageCookie() {
    const name = "lang=";
    const decodedCookie = decodeURIComponent(document.cookie);
    const ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) === 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}