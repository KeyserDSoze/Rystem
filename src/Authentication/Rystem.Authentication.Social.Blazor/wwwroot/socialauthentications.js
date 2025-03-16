window.localStorageFunctions = {
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },
    getItem: function (key) {
        return localStorage.getItem(key);
    }
};

window.languageManager = {
    set: function (language) {
        document.cookie = "lang=" + language + "; path=/";
    },
    get: function () {
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
    },
    getBrowserLanguage: function() {
        return navigator.language || navigator.userLanguage;
    }
}