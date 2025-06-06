document.addEventListener('DOMContentLoaded', function () {
    // Nasłuchiwanie kliknięć w przyciski rozmiaru
    const sizeButtons = document.querySelectorAll('.size-button');
    sizeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const form = button.closest('form');
            const sizeButtons = form.querySelectorAll('.size-button');
            const hiddenInput = form.querySelector('input[name="SizeId"]');
            const addToCartBtn = form.querySelector('button[type="submit"]');
            const sizeId = button.dataset.sizeId;
            const isAvailable = button.dataset.available === 'true';

            // Resetuj style wszystkich przycisków
            sizeButtons.forEach(btn => {
                btn.classList.remove('btn-dark');
                btn.classList.add('btn-outline-dark');
            });

            // Aktualizuj wybrany przycisk i formularz
            if (isAvailable) {
                button.classList.remove('btn-outline-dark');
                button.classList.add('btn-dark');
                hiddenInput.value = sizeId;
                if (addToCartBtn) {
                    addToCartBtn.disabled = false;
                }
            }
        });
    });
});
document.addEventListener('DOMContentLoaded', function () {
    // Przycisk "Dodaj nowy adres"
    const addAddressButton = document.querySelector('.add-address-button');
    if (addAddressButton) {
        addAddressButton.addEventListener('click', clearForm);
    }

    // Przyciski "Edytuj"
    const editAddressButtons = document.querySelectorAll('.edit-address-button');
    editAddressButtons.forEach(button => {
        button.addEventListener('click', function () {
            const id = button.dataset.addressId || '0';
            const addressLine1 = button.dataset.addressLine1 || '';
            const addressLine2 = button.dataset.addressLine2 || '';
            const city = button.dataset.city || '';
            const postalCode = button.dataset.postalCode || '';
            const country = button.dataset.country || '';
            const isDefault = button.dataset.isDefault === 'true';
            loadAddress(id, addressLine1, addressLine2, city, postalCode, country, isDefault);
        });
    });

    function clearForm() {
        console.log('Clearing form');
        document.getElementById('Input_Id').value = '0';
        document.getElementById('Input_AddressLine1').value = '';
        document.getElementById('Input_AddressLine2').value = '';
        document.getElementById('Input_City').value = '';
        document.getElementById('Input_PostalCode').value = '';
        document.getElementById('Input_Country').value = '';
        document.getElementById('Input_IsDefault').checked = false;
        document.querySelector('.modal-title').textContent = 'Dodaj adres';
        document.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
    }

    function loadAddress(id, addressLine1, addressLine2, city, postalCode, country, isDefault) {
        console.log('Loading address: ', id);
        document.getElementById('Input_Id').value = id || '0';
        document.getElementById('Input_AddressLine1').value = addressLine1 || '';
        document.getElementById('Input_AddressLine2').value = addressLine2 || '';
        document.getElementById('Input_City').value = city || '';
        document.getElementById('Input_PostalCode').value = postalCode || '';
        document.getElementById('Input_Country').value = country || '';
        document.getElementById('Input_IsDefault').checked = isDefault || false;
        document.querySelector('.modal-title').textContent = 'Edytuj adres';
        document.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
    }
});
document.addEventListener('DOMContentLoaded', function () {
    // ... istniejący kod z ProductDetails.cshtml, ManageAddresses.cshtml, itp. ...

    // Przyciski rozmiaru dla formularzy "Dodaj do koszyka"
    const sizeButtons = document.querySelectorAll('.size-button');
    sizeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const form = button.closest('form');
            const sizeButtons = form.querySelectorAll('.size-button');
            const hiddenInput = form.querySelector('input[name="SizeId"]');
            const addToCartBtn = form.querySelector('button[type="submit"]');
            const sizeId = button.dataset.sizeId;
            const isAvailable = button.dataset.available === 'true';

            // Resetuj style wszystkich przycisków w formularzu
            sizeButtons.forEach(btn => {
                btn.classList.remove('btn-dark');
                btn.classList.add('btn-outline-dark');
            });

            // Aktualizuj wybrany przycisk i formularz
            if (isAvailable) {
                button.classList.remove('btn-outline-dark');
                button.classList.add('btn-dark');
                hiddenInput.value = sizeId;
                if (addToCartBtn) {
                    addToCartBtn.disabled = false;
                }
            }
        });
    });
});