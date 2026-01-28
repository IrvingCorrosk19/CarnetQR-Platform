# 📚 Documentación del Proyecto CarnetQR Platform

## 📋 Índice de Documentos

### 🚀 Despliegue y Configuración

1. **[GUIA_DESPLIEGUE_DOCKER_ASPNET.md](./GUIA_DESPLIEGUE_DOCKER_ASPNET.md)**
   - **Versión:** 1.0
   - **Descripción:** Guía completa paso a paso para desplegar aplicaciones ASP.NET Core con Docker y PostgreSQL
   - **Incluye:** Configuración de servidor, Docker, docker-compose, DataProtection, cookies, scripts de despliegue
   - **Uso:** Para despliegue inicial de una aplicación

2. **[GUIA_DESPLIEGUE_DOCKER_ASPNET_V2.md](./GUIA_DESPLIEGUE_DOCKER_ASPNET_V2.md)**
   - **Versión:** 2.0 - Multi-App Edition
   - **Descripción:** Versión extendida con Capítulo 18 para múltiples aplicaciones en un solo VPS
   - **Incluye:** Todo lo de la versión 1.0 + configuración multi-app, Nginx, gestión de recursos
   - **Uso:** Para despliegue de múltiples aplicaciones o arquitectura escalable

3. **[COMO_USAR_EL_MANUAL_PARA_OTRA_APP.md](./COMO_USAR_EL_MANUAL_PARA_OTRA_APP.md)**
   - **Descripción:** Guía rápida para adaptar el manual de despliegue a otra aplicación ASP.NET Core
   - **Incluye:** Checklist de ajustes, ejemplos antes/después, búsqueda y reemplazo
   - **Uso:** Cuando quieras usar el manual para una nueva aplicación

### 🧪 Pruebas y Testing

4. **[PLAN_PRUEBAS_COMPLETO.md](./PLAN_PRUEBAS_COMPLETO.md)**
   - **Versión:** 1.0
   - **Descripción:** Plan de pruebas completo con 54 pruebas individuales
   - **Incluye:** Pruebas por rol, CRUD operations, multi-tenancy, impresión, QR codes
   - **Uso:** Para testers con conocimiento técnico

5. **[PLAN_PRUEBAS_DETALLADO.md](./PLAN_PRUEBAS_DETALLADO.md)**
   - **Versión:** 2.0 - Detallado para Testers Sin Conocimiento Técnico
   - **Descripción:** Plan de pruebas extremadamente detallado con instrucciones paso a paso
   - **Incluye:** Instrucciones detalladas, datos de prueba predefinidos, qué buscar en cada paso
   - **Uso:** Para personas sin conocimiento técnico que ejecutan pruebas

### 📊 Análisis y Arquitectura

6. **[ANALISIS_COMPLETO_SISTEMA.md](./ANALISIS_COMPLETO_SISTEMA.md)**
   - **Versión:** 1.0
   - **Descripción:** Análisis exhaustivo y completo del sistema CarnetQR Platform
   - **Incluye:** Arquitectura, modelo de datos, multi-tenancy, seguridad, funcionalidades, infraestructura, puntos fuertes, áreas de mejora, recomendaciones
   - **Uso:** Para desarrolladores, arquitectos, y stakeholders técnicos que necesitan entender el sistema completo

### 🔐 Seguridad y Credenciales

7. **[CREDENCIALES.md](./CREDENCIALES.md)**
   - **Descripción:** Documento con todas las contraseñas y credenciales del sistema
   - **Incluye:** SSH, PostgreSQL, usuarios de aplicación, usuarios de prueba
   - **⚠️ IMPORTANTE:** Este archivo contiene información sensible. NO subir a Git/GitHub
   - **Uso:** Referencia rápida de credenciales

---

## 🎯 Guía Rápida de Uso

### Para Desplegar una Nueva Aplicación:

1. Lee primero: **[COMO_USAR_EL_MANUAL_PARA_OTRA_APP.md](./COMO_USAR_EL_MANUAL_PARA_OTRA_APP.md)**
2. Sigue luego: **[GUIA_DESPLIEGUE_DOCKER_ASPNET.md](./GUIA_DESPLIEGUE_DOCKER_ASPNET.md)** (versión 1.0)
3. Si necesitas múltiples apps: **[GUIA_DESPLIEGUE_DOCKER_ASPNET_V2.md](./GUIA_DESPLIEGUE_DOCKER_ASPNET_V2.md)** (versión 2.0)

### Para Ejecutar Pruebas:

1. Si eres tester técnico: **[PLAN_PRUEBAS_COMPLETO.md](./PLAN_PRUEBAS_COMPLETO.md)**
2. Si eres tester sin conocimiento técnico: **[PLAN_PRUEBAS_DETALLADO.md](./PLAN_PRUEBAS_DETALLADO.md)**

### Para Consultar Credenciales:

- **[CREDENCIALES.md](./CREDENCIALES.md)** (mantener local, no subir a Git)

---

## 📊 Resumen de Documentos

| Documento | Páginas Aprox. | Uso Principal |
|-----------|---------------|-----------------|
| GUIA_DESPLIEGUE_DOCKER_ASPNET.md | ~1600 líneas | Despliegue inicial |
| GUIA_DESPLIEGUE_DOCKER_ASPNET_V2.md | ~2400 líneas | Multi-app, producción |
| COMO_USAR_EL_MANUAL_PARA_OTRA_APP.md | ~400 líneas | Adaptación rápida |
| PLAN_PRUEBAS_COMPLETO.md | ~1000 líneas | Testing técnico |
| PLAN_PRUEBAS_DETALLADO.md | ~1500 líneas | Testing detallado |
| ANALISIS_COMPLETO_SISTEMA.md | ~1200 líneas | Análisis técnico completo |
| CREDENCIALES.md | ~100 líneas | Referencia rápida |

---

## ⚠️ Notas Importantes

### Archivos que NO deben subirse a Git:

- ❌ `CREDENCIALES.md` (contiene contraseñas)
- ❌ Cualquier archivo `.env` (variables de entorno)

### Archivos que SÍ deben subirse a Git:

- ✅ Todos los demás documentos de esta carpeta
- ✅ Scripts PowerShell en `Com/`
- ✅ `Dockerfile`, `docker-compose.yml`
- ✅ `.env.example` (plantilla sin contraseñas)

---

## 🔄 Actualizaciones

- **17 de Enero, 2026:** Creación inicial de documentación
- **17 de Enero, 2026:** Versión 2.0 del manual de despliegue (multi-app)
- **17 de Enero, 2026:** Plan de pruebas detallado para testers sin conocimiento técnico
- **17 de Enero, 2026:** Análisis completo del sistema (arquitectura, seguridad, funcionalidades)

---

**Ubicación:** `Com/Documentacion/`  
**Mantenido por:** Equipo de Desarrollo  
**Última actualización:** 17 de Enero, 2026
