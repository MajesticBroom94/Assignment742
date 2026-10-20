const popup = document.getElementById("bookingPopup");
const popupTitle = document.getElementById("popupTitle");
const popupContent = document.getElementById("popupContent");

function openBookingPopup(title) {
    popupTitle.textContent = title;
    popupContent.replaceChildren();
    popup.hidden = false;
}

function closeBookingPopup() {
    popup.hidden = true;
    popupContent.replaceChildren();
}

// Fetch and display check-in instructions
async function showCheckinSteps(bookingId) {
    openBookingPopup("Check-in Instructions");
    popupContent.textContent = "Loading...";

    try {
        const response = await fetch(
            `/Booking/BookingCheckinSteps?bookingId=${bookingId}`
        );

        if (!response.ok)
            throw new Error("Unable to load instructions.");

        const data = await response.json();

        popupTitle.textContent = data.title;
        popupContent.replaceChildren();

        const list = document.createElement("ol");
        list.className = "popup-steps";

        for (const step of data.steps) {
            const item = document.createElement("li");
            item.textContent = step;
            list.appendChild(item);
        }

        popupContent.appendChild(list);
    }
    catch (error) {
        popupContent.textContent = error.message;
    }
}

function openReviewPopup(bookingId) {
    document.getElementById("reviewBookingId").value = bookingId;
    document.getElementById("reviewPopup").hidden = false;
}

function openDocumentPopup(bookingId) {
    document.getElementById("documentBookingId").value = bookingId;
    document.getElementById("documentPopup").hidden = false;
}

function closeModal(id) {
    document.getElementById(id).hidden = true;
}

// Close when clicking outside the dialog
document.querySelectorAll(".booking-popup-overlay").forEach(overlay => {
    overlay.addEventListener("click", event => {
        if (event.target === overlay) overlay.hidden = true;
    });
});

// Close with Escape
document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
        document.querySelectorAll(".booking-popup-overlay").forEach(overlay => overlay.hidden = true);
        popupContent.replaceChildren();
    }
});