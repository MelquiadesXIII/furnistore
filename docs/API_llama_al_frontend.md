# Confirmación de correo: llamada de la API al frontend

Esta guía describe cómo debe evolucionar el flujo de confirmación de correo para que el enlace recibido por email termine en una página del frontend, en lugar de mostrar directamente una respuesta técnica de la API.

> **Estado actual:** esta integración todavía no está implementada. Actualmente la API confirma el correo y devuelve texto plano. Este documento sirve como contrato y guía para el cambio futuro.

## Objetivo

Cuando el usuario pulse el enlace del correo:

1. La API valida y confirma el correo.
2. La API redirige al frontend.
3. El frontend muestra una página visual de éxito o error.
4. Después de unos segundos, el frontend redirige al destino correspondiente.

El frontend **no debe confirmar el correo por su cuenta** ni llamar primero a otro endpoint. El enlace del email apunta inicialmente a la API porque allí se encuentra el token de confirmación.

## Flujo actual

```text
Correo
  ↓
GET /api/authentication/confirm-email?userId={id}&code={code}
  ↓
API confirma el correo
  ↓
200 OK con texto plano
```

La respuesta exitosa actual es:

```text
Thanks you for confirming your email.
```

Si la solicitud es inválida, la API devuelve `ProblemDetails` con el código HTTP correspondiente.

## Flujo futuro

```text
Correo
  ↓
GET /api/authentication/confirm-email?userId={id}&code={code}
  ↓
API confirma el correo
  ↓
302/303 Redirect al frontend
  ↓
GET /email-confirmation?status=success
  ↓
El frontend muestra la página y redirige automáticamente
```

La lógica de negocio permanece en `AuthService.ConfirmEmailAsync`. El cambio consiste en reemplazar la respuesta de texto plano del controlador por una redirección al frontend.

## Página que debe crear el frontend

Crear una ruta pública:

```text
/email-confirmation
```

La ruta debe aceptar el estado en el query string:

```text
/email-confirmation?status=success
/email-confirmation?status=invalid
/email-confirmation?status=not-found
/email-confirmation?status=failed
```

### Estados de presentación

| Estado | Mensaje sugerido | Acción automática |
|---|---|---|
| `success` | “Tu correo fue confirmado correctamente.” | Redirigir al login |
| `invalid` | “El enlace de confirmación no es válido.” | Redirigir al login o inicio |
| `not-found` | “No encontramos la cuenta asociada al enlace.” | Redirigir al login o inicio |
| `failed` | “No pudimos confirmar tu correo.” | Mostrar botón para continuar |
| Ausente o desconocido | “No se pudo determinar el resultado.” | No asumir éxito; mostrar error controlado |

La página debería incluir:

- indicador visual de éxito o error;
- título y explicación breve;
- contador visible, por ejemplo “Serás redirigido en 5 segundos”;
- botón de acción inmediata, como “Ir a iniciar sesión”;
- diseño responsive;
- estado de carga mientras la aplicación prepara la redirección.

## Contrato de redirección propuesto

La API debería recibir la URL pública del frontend mediante configuración, no tenerla escrita en código:

```text
Frontend__PublicUrl=https://furniture-store.com
```

Tras una confirmación exitosa:

```text
https://furniture-store.com/email-confirmation?status=success
```

Para errores conocidos:

```text
https://furniture-store.com/email-confirmation?status=invalid
https://furniture-store.com/email-confirmation?status=not-found
https://furniture-store.com/email-confirmation?status=failed
```

El backend debe convertir sus errores internos a estados públicos simples. No debe enviar al frontend el token de confirmación, contraseñas, credenciales SMTP ni detalles internos de excepciones.

## Mapeo recomendado de respuestas

| Resultado de la API | Estado para el frontend |
|---|---|
| Confirmación exitosa | `success` |
| Faltan `userId` o `code` | `invalid` |
| El usuario no existe | `not-found` |
| El código no puede decodificarse | `invalid` |
| Identity rechaza la confirmación | `failed` |

La API debe conservar sus respuestas HTTP normales para clientes técnicos. La redirección está pensada para el navegador que abre el enlace del correo.

## Reglas de seguridad

- No guardar `userId` ni `code` en `localStorage`, cookies ni estado persistente.
- No reenviar el token a ninguna otra API después de que la API lo haya procesado.
- No mostrar el token en la interfaz.
- No incluir el token en enlaces de la página final.
- No confiar en `status=success` recibido directamente por el usuario como prueba de confirmación. La confirmación real siempre la realiza la API.
- Validar la URL de redirección en el backend mediante configuración permitida; no aceptar una URL arbitraria desde la solicitud.
- No registrar el query string completo del enlace, porque contiene el token de confirmación.

## Comportamiento del contador

El contador pertenece al frontend y es únicamente una ayuda de navegación. Un comportamiento recomendado es:

1. Mostrar la página inmediatamente.
2. Iniciar una cuenta regresiva de 5 segundos.
3. Redirigir a `/login` cuando llegue a cero.
4. Permitir que el usuario pulse el botón y no tenga que esperar.
5. Si el estado no es `success`, usar `/login` o `/` según la decisión de producto.

La redirección automática no debe volver a llamar al endpoint de confirmación.

## Checklist para frontend

- [ ] Crear la ruta pública `/email-confirmation`.
- [ ] Leer `status` desde los query params.
- [ ] Implementar estados `success`, `invalid`, `not-found` y `failed`.
- [ ] Definir el texto final y el destino de cada estado con producto.
- [ ] Añadir contador y botón de navegación manual.
- [ ] Hacer la vista responsive y accesible.
- [ ] No guardar ni mostrar `userId` o `code`.
- [ ] No llamar nuevamente a `confirm-email` desde la página.
- [ ] Probar el enlace en escritorio y móvil.
- [ ] Probar enlaces válidos, inválidos, incompletos y ya utilizados.

## Checklist para backend antes de activar la integración

- [ ] Configurar `Frontend__PublicUrl` en cada entorno.
- [ ] Reemplazar la respuesta de texto plano por una redirección segura.
- [ ] Mapear los errores de confirmación a los estados públicos definidos.
- [ ] Validar que solo se redirija al frontend configurado.
- [ ] Mantener la confirmación dentro de `AuthService`.
- [ ] Verificar que el token no aparezca en logs ni en la URL final.
- [ ] Actualizar la documentación del endpoint cuando la redirección esté implementada.

## Criterio de aceptación

El cambio estará completo cuando un usuario pueda pulsar el enlace del correo y:

1. vea una página del frontend con el resultado de la operación;
2. reciba un mensaje claro tanto en éxito como en error;
3. pueda continuar manualmente sin esperar;
4. sea redirigido automáticamente después del tiempo definido;
5. no vuelva a ejecutar la confirmación al recargar o navegar desde esa página.
