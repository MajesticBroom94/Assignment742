const stepsContainer = document.getElementById("checkInSteps");
const jsonInput = document.getElementById("CheckInProcessJson");
const stepSummary = document.getElementById("stepSummary");

let steps = [];

// Load existing steps (also works after a validation error).
try {
    const parsed = JSON.parse(jsonInput.value || "[]");

    if (Array.isArray(parsed)) {
        steps = parsed.filter(step => typeof step === "string");
    }
} catch {
    steps = [];
}

function saveSteps() {
    jsonInput.value = JSON.stringify(steps);

    const count = steps.filter(step => step.trim() !== "").length;

    stepSummary.textContent = count === 0
        ? "No check-in steps added."
        : count + " check-in step(s) added.";
}

function renderSteps() {
    stepsContainer.innerHTML = "";

    steps.forEach((step, index) => {

        const row = document.createElement("div");
        row.className = "mb-3";

        const label = document.createElement("label");
        label.className = "form-label";
        label.textContent = "Step " + (index + 1);

        const inputGroup = document.createElement("div");
        inputGroup.className = "input-group";

        const input = document.createElement("input");
        input.type = "text";
        input.className = "form-control";
        input.placeholder = "Enter check-in instruction";
        input.value = step;

        input.addEventListener("input", function () {
            steps[index] = this.value;
            saveSteps();
        });

        const removeBtn = document.createElement("button");
        removeBtn.type = "button";
        removeBtn.className = "btn btn-outline-danger";
        removeBtn.textContent = "Remove";

        removeBtn.addEventListener("click", function () {
            steps.splice(index, 1);
            renderSteps();
        });

        inputGroup.appendChild(input);
        inputGroup.appendChild(removeBtn);

        row.appendChild(label);
        row.appendChild(inputGroup);

        stepsContainer.appendChild(row);
    });

    saveSteps();
}

document.getElementById("addStepBtn")
    .addEventListener("click", function () {
        steps.push("");
        renderSteps();
    });

// Remove empty steps before submitting the form.
document.querySelector('form[asp-action="Create"]')?.addEventListener(
    "submit", saveSteps
);

// Initialise the modal fields and hidden JSON input.
renderSteps();