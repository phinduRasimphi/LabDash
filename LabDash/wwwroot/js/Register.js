document.addEventListener('DOMContentLoaded', function () {
    flatpickr('#dobPicker', {
        dateFormat: 'Y-m-d',
        maxDate: 'today',
        minDate: '1930-01-01',
        allowInput: true,       // lets them type the date directly too
        disableMobile: true     // use flatpickr's UI even on phones, for consistency
    });
});