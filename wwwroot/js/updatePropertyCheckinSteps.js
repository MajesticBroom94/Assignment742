const stepsContainer = document.getElementById("checkInSteps");
const jsonInput = document.getElementById("CheckInProcessJson");
const stepSummary = document.getElementById("stepSummary");
const propertyForm = document.getElementById("updatePropertyForm");

let steps = [];

// Load the existing check-in steps.
try {
    const existing = JSON.parse(jsonInput.value || "[]");

    if (Array.isArray(existing)) {
        steps = existing.filter(s => typeof s === "string");
    }
} catch {
    steps = [];
}

function saveSteps() {
    jsonInput.value = JSON.stringify(steps);

    const count = steps.filter(s => s.trim() !== "").length;

    stepSummary.textContent = count === 0
        ? "No check-in steps added."
        : `${count} check-in step(s) added.`;
}

function renderSteps() {
    stepsContainer.innerHTML = "";

    steps.forEach((step, index) => {
        const row = document.createElement("div");
        row.className = "mb-3";

        const label = document.createElement("label");
        label.className = "form-label";
        label.textContent = `Step ${index + 1}`;

        const group = document.createElement("div");
        group.className = "input-group";

        const input = document.createElement("input");
        input.type = "text";
        input.className = "form-control";
        input.placeholder = "Enter check-in instruction";
        input.value = step;

        input.addEventListener("input", () => {
            steps[index] = input.value;
            saveSteps();
        });

        const removeButton = document.createElement("button");
        removeButton.type = "button";
        removeButton.className = "btn btn-outline-danger";
        removeButton.textContent = "Remove";

        removeButton.addEventListener("click", () => {
            steps.splice(index, 1);
            renderSteps();
        });

        group.appendChild(input);
        group.appendChild(removeButton);

        row.appendChild(label);
        row.appendChild(group);

        stepsContainer.appendChild(row);
    });

    saveSteps();
}

// Add another instruction.
document.getElementById("addStepBtn")
    .addEventListener("click", () => {
        steps.push("");
        renderSteps();

        const inputs = stepsContainer.querySelectorAll("input");
        inputs[inputs.length - 1]?.focus();
    });

// Remove empty instructions before submitting.
propertyForm.addEventListener("submit", () => {
    steps = steps.filter(s => s.trim() !== "");
    saveSteps();
});

renderSteps();