// Profile dropdown
function toggleProfileDropdown() {
    const menu = document.querySelector(".profile-menu");
    const button = document.getElementById("profileButton");

    if (!menu || !button) return;

    const isOpen = menu.classList.toggle("open");
    button.setAttribute("aria-expanded", isOpen);
}

// Close the header dropdown when clicking outside it.
document.addEventListener("click", function (event) {
    const profile = document.querySelector(".profile");

    if (profile && !profile.contains(event.target)) {
        const menu = profile.querySelector(".profile-menu");
        const button = document.getElementById("profileButton");

        if (menu)
            menu.classList.remove("open");

        if (button)
            button.setAttribute("aria-expanded", "false");
    }
});