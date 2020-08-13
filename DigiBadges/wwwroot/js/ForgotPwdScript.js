$(document).ready(function () {
    $('#email').change(function () {
        $('#vemail').val($(this).val());
    });
});
$(document).ready(function () {
    $('#vemail').val($('#email').val());
});
