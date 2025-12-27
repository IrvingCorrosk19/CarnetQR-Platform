// AJAX Helper functions for forms

function submitFormAjax(form, options = {}) {
    var defaults = {
        showLoading: true,
        loadingMessage: 'Procesando...',
        successMessage: 'Operación completada exitosamente',
        successCallback: null,
        errorCallback: null,
        redirectUrl: null
    };

    var settings = Object.assign({}, defaults, options);
    
    // Validar formulario
    if (!$(form).valid()) {
        console.error('Form validation failed');
        return false;
    }

    if (settings.showLoading) {
        showLoading(settings.loadingMessage);
    }

    var formData = new FormData(form);
    var url = $(form).attr('action');
    var method = $(form).attr('method') || 'POST';
    
    // Log frontend - datos que se envían
    console.log('=== FRONTEND AJAX LOG ===');
    console.log('URL:', url);
    console.log('Method:', method);
    var formDataObj = {};
    for (var pair of formData.entries()) {
        formDataObj[pair[0]] = pair[1];
    }
    console.log('FormData contents:', formDataObj);
    console.log('InstitutionId in FormData:', formDataObj['InstitutionId']);
    console.log('========================');

    $.ajax({
        url: url,
        type: method,
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            console.log('=== FRONTEND AJAX SUCCESS ===');
            console.log('Response:', response);
            console.log('=============================');
            
            Swal.close();
            
            if (response.success) {
                showSuccess('Éxito', response.message || settings.successMessage)
                    .then(() => {
                        // Prioridad: redirectUrl del response > redirectUrl de settings > successCallback
                        if (response.redirectUrl) {
                            console.log('Redirecting to:', response.redirectUrl);
                            window.location.href = response.redirectUrl;
                        } else if (settings.redirectUrl) {
                            console.log('Redirecting to (settings):', settings.redirectUrl);
                            window.location.href = settings.redirectUrl;
                        } else if (settings.successCallback) {
                            settings.successCallback(response);
                        } else {
                            // Si no hay redirección ni callback, recargar la página actual
                            console.log('No redirect URL, reloading page');
                            window.location.reload();
                        }
                    });
            } else {
                console.error('=== FRONTEND AJAX ERROR (success=false) ===');
                console.error('Response:', response);
                console.error('Response message:', response.message);
                console.error('Response full:', JSON.stringify(response, null, 2));
                console.error('===========================================');
                
                showError('Error', response.message || 'Ocurrió un error al procesar la solicitud');
                if (settings.errorCallback) {
                    settings.errorCallback(response);
                }
            }
        },
        error: function(xhr, status, error) {
            console.error('=== FRONTEND AJAX ERROR ===');
            console.error('Status:', status);
            console.error('Error:', error);
            console.error('XHR:', xhr);
            console.error('Response Text:', xhr.responseText);
            console.error('Response JSON:', xhr.responseJSON);
            console.error('===========================');
            
            Swal.close();
            var errorMessage = 'Ocurrió un error al procesar la solicitud';
            
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            } else if (xhr.responseText) {
                try {
                    var jsonResponse = JSON.parse(xhr.responseText);
                    if (jsonResponse.message) {
                        errorMessage = jsonResponse.message;
                    }
                } catch (e) {
                    console.error('Error parsing response:', e);
                    // Si no es JSON, usar el mensaje por defecto
                }
            }
            
            showError('Error', errorMessage);
            if (settings.errorCallback) {
                settings.errorCallback(xhr);
            }
        }
    });

    return false;
}

function submitFormAjaxJson(form, data, options = {}) {
    var defaults = {
        showLoading: true,
        loadingMessage: 'Procesando...',
        successMessage: 'Operación completada exitosamente',
        successCallback: null,
        errorCallback: null,
        redirectUrl: null
    };

    var settings = Object.assign({}, defaults, options);
    
    // Validar formulario si tiene validación
    if ($(form).length > 0 && $(form).valid !== undefined && !$(form).valid()) {
        return false;
    }

    if (settings.showLoading) {
        showLoading(settings.loadingMessage);
    }

    var url = $(form).attr('action') || form;
    var method = $(form).attr('method') || 'POST';
    
    // Agregar token antiforgery
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token && typeof data === 'object') {
        data.__RequestVerificationToken = token;
    }

    $.ajax({
        url: url,
        type: method,
        data: typeof data === 'string' ? data : JSON.stringify(data),
        contentType: 'application/json',
        headers: {
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            Swal.close();
            
            if (response.success) {
                showSuccess('Éxito', response.message || settings.successMessage)
                    .then(() => {
                        if (response.redirectUrl || settings.redirectUrl) {
                            window.location.href = response.redirectUrl || settings.redirectUrl;
                        } else if (settings.successCallback) {
                            settings.successCallback(response);
                        }
                    });
            } else {
                showError('Error', response.message || 'Ocurrió un error al procesar la solicitud');
                if (settings.errorCallback) {
                    settings.errorCallback(response);
                }
            }
        },
        error: function(xhr, status, error) {
            Swal.close();
            var errorMessage = 'Ocurrió un error al procesar la solicitud';
            
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            }
            
            showError('Error', errorMessage);
            if (settings.errorCallback) {
                settings.errorCallback(xhr);
            }
        }
    });

    return false;
}

// Setup global AJAX defaults
$.ajaxSetup({
    beforeSend: function(xhr, settings) {
        if (settings.type === 'POST' || settings.type === 'PUT' || settings.type === 'DELETE') {
            var token = $('input[name="__RequestVerificationToken"]').first().val();
            if (token) {
                xhr.setRequestHeader('RequestVerificationToken', token);
            }
        }
    }
});

