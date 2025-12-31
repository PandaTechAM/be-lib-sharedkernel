document.addEventListener('DOMContentLoaded', function () {

    const faviconPath = "/swagger-resources/favicon.svg";

    const existingLink = document.querySelector("link[rel*='icon']");
    if (existingLink) {
        existingLink.href = faviconPath;
    } else {
        const newLink = document.createElement("link");
        newLink.type = "image/svg+xml";
        newLink.rel = "icon";
        newLink.href = faviconPath;
        document.head.appendChild(newLink);
    }
});
