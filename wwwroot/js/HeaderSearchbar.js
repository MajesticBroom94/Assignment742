const propertyDropdown = document.getElementById("propertyDropdown");
const propertyButton = document.getElementById("propertyDropdownButton");
const propertySelected = document.getElementById("propertySelected");
const propertyInput = document.getElementById("propertyType");
const propertyOptions = document.querySelectorAll(".property-option");

const profileButton = document.getElementById("profileButton");
const profileMenu = document.querySelector(".profile-menu");


// Open / close dropdown
propertyButton.addEventListener("click", function (event) {
    event.stopPropagation();
    propertyDropdown.classList.toggle("open");
});

// Select property
propertyOptions.forEach(function (option) {

    option.addEventListener("click", function () {

        const value = this.getAttribute("data-value");
        const text = this.textContent.trim();


        propertySelected.textContent = text;
        propertyInput.value = value;


        // Remove previous selected state
        propertyOptions.forEach(function (item) {
            item.classList.remove("selected");
        });


        // Mark current option
        this.classList.add("selected");

        // Close dropdown
        propertyDropdown.classList.remove("open");

    });

});

/*Close when clicking outside*/
document.addEventListener("click", function (event) {

    if (!propertyDropdown.contains(event.target)) {
        propertyDropdown.classList.remove("open");
    }
});
/* Prevent clicking inside the menu from triggering outside-click behavior*/

propertyDropdown.addEventListener("click", function (event) {
    event.stopPropagation();
});