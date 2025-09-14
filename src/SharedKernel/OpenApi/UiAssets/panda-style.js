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

    // Scroll modal to top when it appears
    const observer = new MutationObserver(() => {
        const modal = document.querySelector('.modal-ux-content');
        if (modal) {
            modal.scrollTop = 0;
            observer.disconnect();
        }
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true,
    });
});
