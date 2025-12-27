# ESPECIFICACIÓN FUNCIONAL – MÓDULO CLÍNICAS Y HOSPITALES

## 1. PROPÓSITO DEL SISTEMA

Sistema de consulta digital mediante código QR, orientado a facilitar el acceso a información de citas médicas y a mejorar la gestión y trazabilidad de la atención, reduciendo consultas presenciales y llamadas telefónicas.

El sistema es contratado y pagado por la institución. El paciente y/o cuidador no paga ni se registra.

## 2. PRINCIPIOS CLAVE (NO NEGOCIABLES)

●El QR es solo de consulta (solo lectura).

●El paciente no interactúa, no edita, no inicia sesión.

●La información se muestra automáticamente al escanear el QR.

●Cada institución es independiente.

## 3. CREACIÓN DE LA INSTITUCIÓN

Responsable: QL Services

QL Services:

●Crea la institución en el sistema.

●Define el tipo: Clínica u Hospital.

●Crea el primer usuario Administrador de la institución.

La institución NO puede:

●Crear otras instituciones.

●Acceder a datos de terceros.

## 4. USUARIOS INTERNOS

### 4.1 Administrador de la institución

Puede:

●Crear y gestionar usuarios internos.

●Configurar visibilidad de datos del paciente (global y por paciente).

●Configurar carnets.

●Consultar estadísticas.

### 4.2 Usuarios operativos / funcionarios de salud

Pueden:

●Crear y editar pacientes.

●Registrar citas médicas.

●Marcar si una atención/tratamiento fue realizada.

No pueden:

●Cambiar configuraciones institucionales.

## 5. GESTIÓN DE PACIENTES

Datos del paciente:

●Datos básicos configurables por institución.

●Número de carnet único y consecutivo.

Numeración:

●Prefijo definido por la institución (ej. HEMO).

●Consecutivo automático.

●Ejemplo: HEMO-0001.

●No editable, no reutilizable.

## 6. GESTIÓN DE CITAS

Registro de cita:

●Fecha.

●Hora.

●Observaciones (opcional).

Estado de la cita:

●Programada.

●Atención realizada.

●Atención no realizada.

Regla:

●La atención solo puede marcarse después de la fecha/hora programada.

●Solo por usuarios internos autorizados.

## 7. EXPERIENCIA AL ESCANEAR EL QR (PACIENTE / CUIDADOR)

Al escanear el QR se muestra:

Identificación

●Nombre del paciente o número de carnet (configurable por institución).

Información institucional

●Logo de la institución.

●Nombre de la institución.

●Información fija (teléfono, dirección, indicaciones).

Citas

●Citas futuras (programadas).

●Historial completo de citas pasadas.

No se permite:

●Edición de datos.

●Confirmaciones.

●Cancelaciones.

## 8. CONFIGURACIÓN DEL CARNET

El administrador puede:

●Seleccionar hasta 6 campos visibles del paciente.

●Definir el orden de los campos.

●Definir si el carnet incluye foto del paciente o no (configuración por institución).

●Importar logo institucional.

●Definir nombre de la institución.

●Importar plantilla o diseño del carnet.

Reglas sobre la foto:

●La inclusión de la foto es opcional y definida por la institución.

●Si la institución activa el uso de foto, los usuarios internos podrán cargar la imagen del paciente.

●Si la institución no activa el uso de foto, el sistema no solicitará ni mostrará imágenes.

El sistema:

●Inserta automáticamente los datos del paciente.

●Inserta el código QR único.

●Inserta la foto del paciente solo si está habilitada.

●Ajusta el diseño final para impresión en PVC.

## 9. MÓDULO DE ESTADÍSTICAS (INTERNO)

Indicadores:

●Total de citas programadas.

●Total de atenciones realizadas.

●Atenciones no realizadas.

●% de cumplimiento.

●Tendencias por período.

Visible solo para usuarios autorizados.

## 10. SEGURIDAD Y PRIVACIDAD

●Acceso QR sin autenticación.

●Información mostrada según configuración institucional.

●Aislamiento total entre instituciones.

●Registro de acciones de usuarios internos.

## 11. ALCANCE

Este módulo NO:

●Gestiona historiales clínicos.

●Integra sistemas externos.

●Permite acciones desde el QR.

## 12. DEFINICIÓN DE ROLES Y PERMISOS

### 12.1 Rol: Administrador de la institución

Responsable de la configuración y control interno del sistema.

Permisos:

●Gestionar datos de la institución.

●Cargar y actualizar logo institucional.

●Definir prefijo del carnet.

●Configurar campos visibles del carnet (hasta 6).

●Importar plantilla/diseño del carnet.

●Configurar visibilidad de datos del paciente (global y por paciente).

●Crear, editar, activar y desactivar usuarios internos.

●Consultar módulo de estadísticas.

Restricciones:

●No puede crear ni eliminar instituciones.

●No puede acceder a datos de otras instituciones.

### 12.2 Rol: Funcionario de salud

Usuario autorizado para la gestión operativa de pacientes y citas.

Permisos:

●Crear y editar pacientes.

●Registrar citas médicas.

●Marcar atención/tratamiento como realizado o no realizado (solo después de la fecha/hora de la cita).

●Consultar información interna necesaria para su labor.

Restricciones:

●No puede modificar configuraciones institucionales.

●No puede alterar numeración de carnets.

●No puede eliminar historiales.

### 12.3 Rol: Operador administrativo (opcional)

Usuario de apoyo administrativo.

Permisos:

●Crear pacientes.

●Registrar citas.

●Consultar información interna.

Restricciones:

●No puede marcar atención/tratamiento.

●No puede configurar carnets ni estadísticas.

## 13. CONSIDERACIONES PARA DESARROLLO

●El sistema debe ser multi institución.

●Los permisos deben ser controlados por rol.

●Todas las acciones internas deben quedar registradas (auditoría básica).

●El módulo de estadísticas depende únicamente de los registros de citas y atenciones.

