async function wishlistManager(propertyId, button) {
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        const response = await fetch(
            `/Wishlist/HandleStatus`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    'RequestVerificationToken': token},
                body: JSON.stringify(propertyId)
            }
        );

        if (!response.ok) {
            throw new Error("Response Error");
        }

        const result = await response.json();

        if (result.success) {
            if (result.message === "added") {
                button.classList.add('active');
                button.textContent = '♥';
            } 
            
            else if (result.message === "removed") {
                button.classList.remove('active');
                button.textContent = '♡';
            }
        } else {
            const div = `     
                    <div class="custom-popup custom-popup-error">
                    <div class="custom-popup-icon">
                        <i class="fa fa-exclamation-circle"></i>
                    </div>
            
                    <div class="custom-popup-content">
                        <strong>Could Not Wishlist</strong>
                        <span>Please Log In To Add To Your Wishlist</span>
                    </div>
                </div>`;
            
            document.body.insertAdjacentHTML("afterbegin", div);
            closePopup();
        }

    } catch (error) {
        console.log(error);
        alert("Could not change wishlist item status");
    }
}

function closePopup() {
    document.querySelectorAll(".custom-popup").forEach(popup => {

        function closePopup() {

            popup.classList.add("popup-closing");

            popup.addEventListener(
                "animationend",
                () => popup.remove(),
                { once: true }
            );
        }

        // Automatically disappear after timeout
        setTimeout(closePopup, 5000);
    });
}
