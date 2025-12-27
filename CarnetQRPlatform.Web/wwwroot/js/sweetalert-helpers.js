// Helper functions for SweetAlert2

function showLoading(message = 'Procesando...') {
    Swal.fire({
        title: message,
        allowOutsideClick: false,
        allowEscapeKey: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
}

function showSuccess(title = 'Éxito', message = 'Operación completada exitosamente', callback = null) {
    return Swal.fire({
        icon: 'success',
        title: title,
        text: message,
        confirmButtonText: 'Aceptar',
        timer: 3000,
        timerProgressBar: true
    }).then((result) => {
        if (callback) callback(result);
        return result;
    });
}

function showError(title = 'Error', message = 'Ocurrió un error al procesar la solicitud', callback = null) {
    return Swal.fire({
        icon: 'error',
        title: title,
        text: message,
        confirmButtonText: 'Aceptar'
    }).then((result) => {
        if (callback) callback(result);
        return result;
    });
}

function showWarning(title = 'Advertencia', message = '', callback = null) {
    Swal.fire({
        icon: 'warning',
        title: title,
        text: message,
        confirmButtonText: 'Aceptar',
        cancelButtonText: 'Cancelar',
        showCancelButton: true
    }).then((result) => {
        if (callback) callback(result);
    });
}

function showConfirm(title = '¿Está seguro?', message = 'Esta acción no se puede deshacer', confirmText = 'Sí, continuar', cancelText = 'Cancelar') {
    return Swal.fire({
        icon: 'question',
        title: title,
        text: message,
        showCancelButton: true,
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    });
}

// Interceptar formularios para mostrar loading
$(document).ready(function() {
    // Interceptar submits de formularios con clase 'swal-submit'
    $('form.swal-submit').on('submit', function(e) {
        if ($(this).valid() || !$(this).find('.input-validation-error').length) {
            var submitText = $(this).find('button[type="submit"]').text().trim() || 'Guardando...';
            showLoading(submitText);
        }
    });
});

