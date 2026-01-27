document.addEventListener('DOMContentLoaded', function () {
    // Set custom favicon
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

    // Fix auth modal scroll position - ensures modal opens at top
    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === 1) {
                    const modalContent = node.querySelector?.('.modal-ux-content')
                        || (node.classList?.contains('modal-ux-content') ? node : null);
                    if (modalContent) {
                        modalContent.scrollTop = 0;
                    }
                }
            });
        });
    });

    observer.observe(document.body, { childList: true, subtree: true });
});