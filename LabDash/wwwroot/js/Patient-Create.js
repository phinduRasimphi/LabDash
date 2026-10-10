document.addEventListener("DOMContentLoaded", async function () {
    // =========================================================
    // ADDRESS DROPDOWNS
    // Province -> City -> Suburb -> Postal Code
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

            if (!response.ok) {
                throw new Error("Could not load dropdown data.");
            }

            const values = await response.json();

            values.forEach(value => {
                const option = document.createElement("option");
                option.value = value;
                option.textContent = value;

                if (selectedValue && value === selectedValue) {
                    option.selected = true;
                }

                select.appendChild(option);
            });

            select.disabled = values.length === 0;

            return values;
        } catch (error) {
            console.error(error);
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
        });

        citySelect.addEventListener("change", async function () {
            await loadSuburbs(
                provinceSelect.value,
                citySelect.value,
                null
            );
        });

        suburbSelect.addEventListener("change", async function () {
            await loadPostalCodes(
                provinceSelect.value,
                citySelect.value,
                suburbSelect.value,
                null
            );
        });

        // Rebuild dependent dropdowns after a validation error.
        // Preserve values rendered by ASP.NET Core.
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
        }
    }

    // =========================================================
    // MEDICAL CATEGORY DROPDOWNS
    // =========================================================

    function setupMedicalCategoryDropdown(categoryId, itemId) {
        const categorySelect = document.getElementById(categoryId);
        const itemSelect = document.getElementById(itemId);

        if (!categorySelect || !itemSelect) {
            return;
        }

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

            itemSelect.disabled =
                selectedCategory === "" || filtered.length === 0;

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

    // =========================================================
    // AUTO-POPULATE DATE OF BIRTH FROM SA ID NUMBER
    // =========================================================

    const idNumberInput = document.getElementById("IDNumber");
    const dobInput = document.getElementById("DOB");

    function populateDobFromId() {
        if (!idNumberInput || !dobInput) {
            return;
        }

        const id = idNumberInput.value.trim();

        // SA ID must be exactly 13 digits
        if (id.length !== 13 || !/^\d{13}$/.test(id)) {
            return;
        }

        const year2 = parseInt(id.substring(0, 2), 10);
        const month = parseInt(id.substring(2, 4), 10);
        const day = parseInt(id.substring(4, 6), 10);

        // Determine century: if the 2-digit year is greater than the
        // current 2-digit year, it must be 1900s; otherwise 2000s.
        const currentYY = new Date().getFullYear() % 100;
        const century = year2 > currentYY ? 1900 : 2000;
        const fullYear = century + year2;

        // Basic sanity check on month and day
        if (month < 1 || month > 12) {
            return;
        }

        // Validate the day against the month (handles leap years)
        const daysInMonth = new Date(fullYear, month, 0).getDate();
        if (day < 1 || day > daysInMonth) {
            return;
        }

        // HTML date inputs expect yyyy-MM-dd
        const pad = (n) => n.toString().padStart(2, "0");
        dobInput.value = `${fullYear}-${pad(month)}-${pad(day)}`;
    }

    if (idNumberInput) {
        // Trigger on blur (when the user leaves the field)
        idNumberInput.addEventListener("blur", populateDobFromId);

        // Also trigger when the field reaches 13 digits, without waiting for blur
        idNumberInput.addEventListener("input", function () {
            const v = idNumberInput.value.trim();
            if (v.length === 13 && /^\d{13}$/.test(v)) {
                populateDobFromId();
            }
        });
    }
});