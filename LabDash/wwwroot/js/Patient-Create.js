// =============================================================
//  LabDash — Create Patient Form
// =============================================================

document.addEventListener("DOMContentLoaded", async function () {

    const form = document.getElementById("createPatientForm");
    if (!form) {
        console.warn("patient-create.js: form #createPatientForm not found");
        return;
    }

    const submitBtn = document.getElementById("registerPatientBtn");
    if (!submitBtn) {
        console.warn("patient-create.js: button #registerPatientBtn not found");
        return;
    }

    const summary = document.getElementById("validationSummary");
    const summaryList = document.getElementById("validationSummaryList");

    const requiredFields = Array.from(
        form.querySelectorAll("[data-required='true']")
    );

    const optionalValidatedFields = Array.from(
        form.querySelectorAll("[data-required='false'][data-validate]")
    );

    const allValidatedFields = requiredFields.concat(optionalValidatedFields);

    // -----------------------------------------------------------
    //  Field state helpers
    // -----------------------------------------------------------

    function errorSpanFor(field) {
        const wrap = field.closest(".np-field") ||
            field.closest(".patient-field") ||
            field.parentElement;
        return wrap ? wrap.querySelector(".np-error, .field-validation") : null;
    }

    function setFieldError(field, message) {
        field.classList.remove("is-valid");
        field.classList.add("is-invalid");
        const span = errorSpanFor(field);
        if (span) span.textContent = message || "";
    }

    function setFieldValid(field) {
        field.classList.remove("is-invalid");
        field.classList.add("is-valid");
        const span = errorSpanFor(field);
        if (span) span.textContent = "";
    }

    function clearFieldState(field) {
        field.classList.remove("is-valid", "is-invalid");
        const span = errorSpanFor(field);
        if (span) span.textContent = "";
    }

    // -----------------------------------------------------------
    //  Validators
    // -----------------------------------------------------------

    const validators = {

        name: function (value) {
            if (!value) return "This field is required.";
            if (value.length < 2) return "Must be at least 2 characters.";
            if (value.length > 60) return "Must be 60 characters or fewer.";
            if (!/^[A-Za-z][A-Za-z' -]*$/.test(value))
                return "Only letters, spaces, apostrophes and hyphens are allowed.";
            return null;
        },

        email: function (value) {
            if (!value) return "Email address is required.";
            const re = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
            if (!re.test(value))
                return "Enter a valid email address (e.g. name@example.com).";
            return null;
        },

        cellphone: function (value) {
            if (!value) return "Cellphone number is required.";
            if (!/^\d+$/.test(value))
                return "Cellphone number must contain digits only.";
            if (value.length !== 10)
                return "Cellphone number must be exactly 10 digits.";
            if (!/^0[6-8]/.test(value))
                return "Must be a valid SA mobile number (starts with 06, 07 or 08).";
            return null;
        },

        said: function (value) {
            if (!value) return "SA ID number is required.";
            if (!/^\d+$/.test(value))
                return "SA ID number must contain digits only.";
            if (value.length !== 13)
                return "SA ID number must be exactly 13 digits.";
            return null; // Removed Luhn check
        },

        dob: function (value) {
            if (!value) return "Date of birth is required.";
            const dob = new Date(value);
            if (isNaN(dob.getTime())) return "Enter a valid date.";
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            if (dob > today) return "Date of birth cannot be in the future.";
            const age = (today - dob) / (1000 * 60 * 60 * 24 * 365.25);
            if (age > 120) return "Please enter a realistic date of birth.";
            return null;
        },

        select: function (value) {
            if (!value) return "Please make a selection.";
            return null;
        },

        address: function (value) {
            if (!value) return "Address Line 1 is required.";
            if (value.length < 3) return "Address is too short.";
            if (value.length > 120) return "Address must be 120 characters or fewer.";
            return null;
        }
    };

    // -----------------------------------------------------------
    //  Field validation
    // -----------------------------------------------------------

    function validateField(field) {
        const value = (field.value || "").trim();
        const isRequired = field.dataset.required === "true";
        const validatorName = field.dataset.validate;

        if (isRequired && !value) {
            setFieldError(field, "This field is required.");
            return false;
        }

        if (!isRequired && !value) {
            clearFieldState(field);
            return true;
        }

        if (validatorName && validators[validatorName]) {
            const err = validators[validatorName](value);
            if (err) {
                setFieldError(field, err);
                return false;
            }
        }

        setFieldValid(field);
        return true;
    }

    // -----------------------------------------------------------
    //  Cross-check DOB against SA ID date block
    // -----------------------------------------------------------

    function crossValidateDobWithId() {
        const idField = form.querySelector("[data-validate='said']");
        const dobField = form.querySelector("[data-validate='dob']");
        if (!idField || !dobField) return;

        const id = (idField.value || "").trim();
        const dob = dobField.value;
        if (id.length !== 13 || !dob) return;

        const yy = parseInt(id.substring(0, 2), 10);
        const mm = parseInt(id.substring(2, 4), 10);
        const dd = parseInt(id.substring(4, 6), 10);

        const currentYY = new Date().getFullYear() % 100;
        const century = yy > currentYY ? 1900 : 2000;
        const year = century + yy;

        const pad = n => n.toString().padStart(2, "0");
        const expected = `${year}-${pad(mm)}-${pad(dd)}`;

        if (expected !== dob) {
            setFieldError(
                dobField,
                `Date of birth does not match the SA ID number (expected ${expected}).`
            );
        } else {
            setFieldValid(dobField);
        }
    }

    // -----------------------------------------------------------
    //  Global validity + submit button gating
    // -----------------------------------------------------------

    function allRequiredValid() {
        let ok = true;
        requiredFields.forEach(field => {
            const value = (field.value || "").trim();
            const validatorName = field.dataset.validate;
            if (!value) { ok = false; return; }
            if (validatorName && validators[validatorName]) {
                if (validators[validatorName](value)) ok = false;
            }
        });
        return ok;
    }

    function refreshSubmitState() {
        submitBtn.disabled = !allRequiredValid();
    }

    // -----------------------------------------------------------
    //  Error summary
    // -----------------------------------------------------------

    function buildSummary() {
        if (!summary || !summaryList) return;
        summaryList.innerHTML = "";
        let count = 0;

        requiredFields.forEach(field => {
            const value = (field.value || "").trim();
            const label = field.dataset.label || field.name;
            const validatorName = field.dataset.validate;
            let error = null;

            if (!value) {
                error = "is required";
            } else if (validatorName && validators[validatorName]) {
                error = validators[validatorName](value);
            }

            if (error) {
                const li = document.createElement("li");
                li.innerHTML = `<strong>${label}</strong> — ${error}`;
                summaryList.appendChild(li);
                count++;
            }
        });

        summary.classList.toggle("is-visible", count > 0);
    }

    // -----------------------------------------------------------
    //  Attach listeners
    // -----------------------------------------------------------

    allValidatedFields.forEach(field => {
        field.addEventListener("blur", function () {
            validateField(field);
            if (field.dataset.validate === "said" || field.dataset.validate === "dob") {
                crossValidateDobWithId();
            }
            refreshSubmitState();
        });

        field.addEventListener("input", function () {
            if (field.classList.contains("is-invalid") ||
                field.classList.contains("is-valid")) {
                validateField(field);
                if (field.dataset.validate === "said" ||
                    field.dataset.validate === "dob") {
                    crossValidateDobWithId();
                }
            }
            refreshSubmitState();
            if (summary && summary.classList.contains("is-visible")) {
                buildSummary();
            }
        });

        field.addEventListener("change", function () {
            validateField(field);
            refreshSubmitState();
        });
    });

    // -----------------------------------------------------------
    //  Digits-only enforcement (SA ID + cellphone)
    // -----------------------------------------------------------

    ["said", "cellphone"].forEach(kind => {
        form.querySelectorAll(`[data-validate='${kind}']`).forEach(input => {
            input.addEventListener("input", function () {
                const cleaned = input.value.replace(/\D/g, "");
                if (cleaned !== input.value) input.value = cleaned;
            });
            input.addEventListener("keypress", function (e) {
                if (e.key.length === 1 && !/\d/.test(e.key)) {
                    e.preventDefault();
                }
            });
            input.addEventListener("paste", function (e) {
                const text = (e.clipboardData || window.clipboardData).getData("text");
                if (!/^\d+$/.test(text)) {
                    e.preventDefault();
                    const digits = text.replace(/\D/g, "");
                    document.execCommand("insertText", false, digits);
                }
            });
        });
    });

    // -----------------------------------------------------------
    //  Auto-populate DOB from SA ID
    // -----------------------------------------------------------

    const idNumberInput = document.getElementById("IDNumber");
    const dobInput = document.getElementById("DOB");

    function populateDobFromId() {
        if (!idNumberInput || !dobInput) return;

        const id = idNumberInput.value.trim();
        if (id.length !== 13 || !/^\d{13}$/.test(id)) return;

        // Luhn check removed here so the DOB always populates

        const yy = parseInt(id.substring(0, 2), 10);
        const mm = parseInt(id.substring(2, 4), 10);
        const dd = parseInt(id.substring(4, 6), 10);

        const currentYY = new Date().getFullYear() % 100;
        const century = yy > currentYY ? 1900 : 2000;
        const year = century + yy;

        const daysInMonth = new Date(year, mm, 0).getDate();
        if (mm < 1 || mm > 12 || dd < 1 || dd > daysInMonth) return;

        const pad = n => n.toString().padStart(2, "0");
        dobInput.value = `${year}-${pad(mm)}-${pad(dd)}`;
        setFieldValid(dobInput);
        refreshSubmitState();
    }

    if (idNumberInput) {
        idNumberInput.addEventListener("blur", populateDobFromId);
        idNumberInput.addEventListener("input", function () {
            const v = idNumberInput.value.trim();
            if (v.length === 13 && /^\d{13}$/.test(v)) {
                populateDobFromId();
            }
        });
    }

    // -----------------------------------------------------------
    //  Submit gate
    // -----------------------------------------------------------

    form.addEventListener("submit", function (e) {
        allValidatedFields.forEach(validateField);
        crossValidateDobWithId();

        if (!allRequiredValid()) {
            e.preventDefault();
            buildSummary();
            if (summary) summary.scrollIntoView({ behavior: "smooth", block: "start" });
            const firstInvalid = form.querySelector(".is-invalid");
            if (firstInvalid) firstInvalid.focus();
            return;
        }

        submitBtn.disabled = true;
        submitBtn.innerHTML =
            '<i class="fas fa-spinner fa-spin"></i> Registering...';
    });

    // -----------------------------------------------------------
    //  Initial state
    // -----------------------------------------------------------

    refreshSubmitState();

    // =========================================================
    //  ADDRESS CASCADE — Province → City → Suburb → Postal Code
    // =========================================================

    const provinceSelect = document.getElementById("Province");
    const citySelect = document.getElementById("City");
    const suburbSelect = document.getElementById("Suburb");
    const postalCodeSelect = document.getElementById("PostalCode");

    async function fetchOptions(url, select, placeholder, selectedValue) {
        select.innerHTML = "";
        const firstOption = document.createElement("option");
        firstOption.value = "";
        firstOption.textContent = placeholder;
        select.appendChild(firstOption);

        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error("Could not load dropdown data.");
            const values = await response.json();

            values.forEach(value => {
                const option = document.createElement("option");
                option.value = value;
                option.textContent = value;
                if (selectedValue && value === selectedValue) option.selected = true;
                select.appendChild(option);
            });

            select.disabled = values.length === 0;
            return values;
        } catch (error) {
            console.error("fetchOptions:", error);
            select.disabled = true;
            return [];
        }
    }

    function clearSelect(select, placeholder) {
        if (!select) return;
        select.innerHTML = "";
        const option = document.createElement("option");
        option.value = "";
        option.textContent = placeholder;
        select.appendChild(option);
        select.disabled = true;
    }

    async function loadCities(province, selectedCity) {
        clearSelect(suburbSelect, "Select Suburb");
        clearSelect(postalCodeSelect, "Select Postal Code");
        if (!province) {
            clearSelect(citySelect, "Select City / Town");
            return;
        }
        await fetchOptions(
            `/Doctor/GetCities?province=${encodeURIComponent(province)}`,
            citySelect,
            "Select City / Town",
            selectedCity
        );
    }

    async function loadSuburbs(province, city, selectedSuburb) {
        clearSelect(postalCodeSelect, "Select Postal Code");
        if (!province || !city) {
            clearSelect(suburbSelect, "Select Suburb");
            return;
        }
        await fetchOptions(
            `/Doctor/GetSuburbs?province=${encodeURIComponent(province)}&city=${encodeURIComponent(city)}`,
            suburbSelect,
            "Select Suburb",
            selectedSuburb
        );
    }

    async function loadPostalCodes(province, city, suburb, selectedPostalCode) {
        if (!province || !city || !suburb) {
            clearSelect(postalCodeSelect, "Select Postal Code");
            return;
        }
        await fetchOptions(
            `/Doctor/GetPostalCodes?province=${encodeURIComponent(province)}&city=${encodeURIComponent(city)}&suburb=${encodeURIComponent(suburb)}`,
            postalCodeSelect,
            "Select Postal Code",
            selectedPostalCode
        );
    }

    if (provinceSelect && citySelect && suburbSelect && postalCodeSelect) {
        provinceSelect.addEventListener("change", async function () {
            await loadCities(provinceSelect.value, null);
            validateField(provinceSelect);
            refreshSubmitState();
        });

        citySelect.addEventListener("change", async function () {
            await loadSuburbs(provinceSelect.value, citySelect.value, null);
            validateField(citySelect);
            refreshSubmitState();
        });

        suburbSelect.addEventListener("change", async function () {
            await loadPostalCodes(
                provinceSelect.value,
                citySelect.value,
                suburbSelect.value,
                null
            );
            validateField(suburbSelect);
            refreshSubmitState();
        });

        postalCodeSelect.addEventListener("change", function () {
            validateField(postalCodeSelect);
            refreshSubmitState();
        });

        const savedCity = citySelect.dataset.currentValue || citySelect.value;
        const savedSuburb = suburbSelect.dataset.currentValue || suburbSelect.value;
        const savedPostalCode = postalCodeSelect.dataset.currentValue || postalCodeSelect.value;
        const initialProvince = provinceSelect.value;

        if (initialProvince) {
            const cityValues = await fetchOptions(
                `/Doctor/GetCities?province=${encodeURIComponent(initialProvince)}`,
                citySelect,
                "Select City / Town",
                savedCity
            );

            if (savedCity && cityValues.includes(savedCity)) {
                const suburbValues = await fetchOptions(
                    `/Doctor/GetSuburbs?province=${encodeURIComponent(initialProvince)}&city=${encodeURIComponent(savedCity)}`,
                    suburbSelect,
                    "Select Suburb",
                    savedSuburb
                );

                if (savedSuburb && suburbValues.includes(savedSuburb)) {
                    await fetchOptions(
                        `/Doctor/GetPostalCodes?province=${encodeURIComponent(initialProvince)}&city=${encodeURIComponent(savedCity)}&suburb=${encodeURIComponent(savedSuburb)}`,
                        postalCodeSelect,
                        "Select Postal Code",
                        savedPostalCode
                    );
                }
            }
            refreshSubmitState();
        }
    }

    // =========================================================
    //  MEDICAL DROPDOWNS — Category → Item
    // =========================================================

    function setupMedicalCategoryDropdown(categoryId, itemId) {
        const categorySelect = document.getElementById(categoryId);
        const itemSelect = document.getElementById(itemId);
        if (!categorySelect || !itemSelect) return;

        const originalOptions = Array.from(itemSelect.options)
            .filter(option => option.value !== "")
            .map(option => ({
                value: option.value,
                text: option.textContent.trim(),
                category: (option.dataset.category || "").trim()
            }));

        function updateItems() {
            const selectedCategory = categorySelect.value.trim();
            const previousValue = itemSelect.value;

            itemSelect.innerHTML = "";
            const placeholder = document.createElement("option");
            placeholder.value = "";
            placeholder.textContent = selectedCategory
                ? "Select an option"
                : "Select a category first";
            itemSelect.appendChild(placeholder);

            const filtered = originalOptions.filter(option =>
                selectedCategory !== "" &&
                option.category.toLowerCase() === selectedCategory.toLowerCase()
            );

            filtered.forEach(data => {
                const option = document.createElement("option");
                option.value = data.value;
                option.textContent = data.text;
                option.dataset.category = data.category;
                itemSelect.appendChild(option);
            });

            itemSelect.disabled = selectedCategory === "" || filtered.length === 0;

            if (filtered.some(option => option.value === previousValue)) {
                itemSelect.value = previousValue;
            } else {
                itemSelect.value = "";
            }
        }

        categorySelect.addEventListener("change", updateItems);
        updateItems();
    }

    setupMedicalCategoryDropdown("conditionCategory", "conditionItem");
    setupMedicalCategoryDropdown("allergyCategory", "allergyItem");
    setupMedicalCategoryDropdown("medicationCategory", "medicationItem");
});