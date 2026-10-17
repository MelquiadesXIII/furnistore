# Furnistore: 30 preguntas sobre el negocio

Preguntas que nos pueden hacer en la defensa sobre el negocio que modela Furnistore, con la respuesta que debemos saber dar. Las respuestas explican **qué** decidimos y **por qué**, y cuando ayuda, **dónde se ve** en la aplicación o en el código. Los números RN y RF son los del informe.

---

## El negocio y sus actores

### 1. ¿Qué negocio modela Furnistore y qué problema resuelve?

Una tienda de muebles que vende en línea en Cuba. El cliente puede ver el catálogo, comparar precios, medidas y materiales, y comprar sin ir a la tienda. La tienda, por su parte, controla en un solo sitio su catálogo, su stock, sus pedidos hasta la entrega y sus clientes, y obtiene reportes para decidir qué reponer, qué se vende y si está cumpliendo con las entregas. Hasta ahora esa información estaría repartida entre libretas, hojas de cálculo y mensajes.

### 2. ¿Quiénes son los actores y qué puede hacer cada uno?

- **Visitante** (sin cuenta): ve el catálogo, busca y abre la ficha de cada mueble.
- **Cliente** (cuenta con el correo confirmado): además usa el carrito, compra, sigue sus pedidos y puede cancelar uno mientras está *Pagado*.
- **Administrador** (un cliente con rol `Admin`): gestiona productos, categorías, pedidos y clientes, consulta reportes y revisa la auditoría.

El administrador es un cliente con un permiso más, no otro tipo de usuario: así una misma persona puede comprar y administrar con una sola cuenta.

### 3. ¿Por qué cualquiera puede ver el catálogo, pero hace falta una cuenta para comprar?

Porque mirar no compromete a nada y es lo que atrae al cliente: obligar a registrarse para ver precios espantaría a la mayoría. Comprar, en cambio, sí compromete: hay que saber quién compra, adónde se entrega y a quién llamar. Por eso el carrito, el checkout y los pedidos exigen sesión, y el botón «Agregar al carrito» lleva al login a quien no la tiene.

### 4. ¿Por qué hay que confirmar el correo antes de poder entrar?

Para asegurarnos de que el correo existe y es de quien se registra. Es nuestro canal de contacto, y el correo identifica la cuenta. Sin confirmarlo, cualquiera podría registrarse con el correo de otra persona. El código tiene 6 dígitos, vence a los 15 minutos, admite 5 intentos fallidos y solo se puede reenviar una vez por minuto (RN2), para que no se pueda adivinar por fuerza bruta ni usar la tienda para llenar de correos a alguien.

### 5. ¿Cómo se convierte alguien en administrador?

Nunca registrándose: el registro siempre crea clientes. Un administrador existente da el rol desde la ficha del cliente, en el panel, y esa acción queda en la auditoría. El primer administrador se crea desde la consola del servidor (`dotnet run --project API.Furnistore.API -- admin promote <correo>`), porque al principio no existe nadie en el panel que pueda darlo. Además, solo puede ser administradora una cuenta con el correo confirmado.

---

## Clientes e identidad

### 6. ¿Por qué pedimos el carnet de identidad, y por qué ya al registrarse?

Vendemos muebles: artículos de valor que se entregan en un domicilio. El carnet identifica de forma única a la persona que compra y permite comprobar, al entregar, que recibe quien compró. Lo pedimos desde el registro para que cada cuenta quede ligada a una persona desde el principio: como el carnet es único (RN6), una persona no puede abrir varias cuentas y la tienda sabe con quién está tratando antes del primer pedido.

### 7. ¿Cómo validan el carnet?

Comprobamos lo que se puede comprobar sin consultar ningún registro oficial:

- que tenga exactamente 11 dígitos;
- que los 6 primeros (AAMMDD) formen una fecha que exista; el séptimo indica el siglo: 9 para el XIX, del 6 al 8 para el XXI y el resto para el XX;
- que esa fecha no sea futura;
- que no esté registrado en otra cuenta.

La validación está en la web, para avisar enseguida, y en la API, que es la regla real. El dígito 11 es de control, pero no conocemos la fórmula oficial y preferimos no inventarla: rechazaríamos carnets reales.

### 8. ¿Por qué hace falta el perfil completo para comprar?

Porque sin esos datos el pedido no se puede entregar. La dirección (calle, ciudad y provincia) dice adónde llevarlo, el teléfono sirve para coordinar la entrega y el carnet, para identificar a quien lo recibe (RN7). Si falta algo, el checkout lo pide antes de confirmar, en la misma pantalla.

### 9. ¿Puede una misma persona tener varias cuentas?

No. El correo es único (RN1) y el carnet también (RN6): una segunda cuenta necesitaría otro correo **y** otro carnet. Si alguien lo intenta, el registro le responde «Ese carnet de identidad ya está registrado en otra cuenta».

---

## Catálogo y productos

### 10. ¿Por qué no se puede borrar un producto que ya se vendió?

Porque forma parte de la historia de la tienda. Los pedidos antiguos lo referencian, y los reportes de ventas y productos lo cuentan. Si se borrara, esos datos quedarían cojos o cambiarían hacia atrás. Por eso se **archiva**: desaparece del catálogo, pero los pedidos y los reportes siguen intactos (RN10). Solo se puede borrar un producto que nunca se vendió, por ejemplo uno creado por error. La base de datos refuerza la regla: no deja borrar un producto con pedidos aunque alguien lo intente fuera de la aplicación.

### 11. ¿Por qué los productos agotados siguen apareciendo en la tienda?

Porque un producto agotado sigue siendo parte de la oferta: el cliente sabe que lo vendemos y puede volver cuando lo repongamos. Si desapareciera, parecería que ya no lo vendemos. Se muestra con la etiqueta *Agotado* y no se puede agregar al carrito (RN9 y RN12). Cuando un producto deja de venderse de verdad, se archiva, y entonces sí desaparece.

### 12. ¿En qué moneda vende la tienda y por qué?

En dólares (USD), tanto en el catálogo como en los pedidos y en los reportes (RN20). Usamos una moneda estable: con precios en pesos habría que reajustar el catálogo constantemente por la inflación, y los reportes que comparan meses distintos dejarían de ser comparables. La moneda está definida en un solo sitio (`StoreCalendar.Currency` en la API y `formatPrice` en la web), así que cambiarla no obliga a tocar el resto del sistema.

### 13. Si cambia el precio de un producto, ¿cambian los pedidos ya hechos?

No. Cada pedido guarda una copia del momento de la compra: el nombre y el precio unitario de cada producto, y la dirección de entrega (RN14). Si mañana una silla sube de precio o el cliente se muda, sus pedidos anteriores siguen diciendo exactamente lo que compró, cuánto pagó y adónde se envió. Es lo que se espera de un comprobante de compra.

### 14. ¿Y si el precio cambia mientras el cliente tiene el producto en el carrito?

El carrito no congela precios: siempre muestra el precio actual. Para que nadie pague algo distinto de lo que vio, la web envía al confirmar el total que el cliente tiene en pantalla, y la API lo compara con el total real. Si no coinciden, rechaza la compra con «Los precios de tu carrito cambiaron» y le muestra el nuevo total para que confirme otra vez (RN13). Nunca se cobra una cantidad que el cliente no vio.

---

## La compra

### 15. Describan una compra de principio a fin.

1. El cliente busca en el catálogo y agrega muebles al carrito, sin pasar del stock disponible.
2. En el checkout revisa sus datos de entrega, que deben estar completos.
3. Al confirmar, la API, en una sola transacción:
   - comprueba de nuevo el stock y el total;
   - descuenta el stock;
   - crea el pedido en estado *Pagado*, con su número, una copia de los precios y de la dirección, y una fecha estimada de entrega a 7 días;
   - vacía el carrito y registra el pedido en la auditoría.
4. A partir de ahí la tienda lo prepara, lo envía y lo marca como entregado, y el cliente ve cada paso en «Mis pedidos».

### 16. ¿Cómo garantizan que no se vende más de lo que hay en el almacén?

Con tres barreras:

1. El carrito no deja agregar más unidades que el stock.
2. El checkout vuelve a comprobarlo, porque desde que se agregó al carrito otro cliente pudo comprar.
3. El descuento de stock es condicional y atómico: la base solo resta si todavía queda suficiente. Si dos clientes intentan comprar la última unidad a la vez, uno lo consigue y el otro recibe un aviso de stock insuficiente.

Además, la base de datos no admite stock negativo.

### 17. ¿Agregar un producto al carrito lo reserva?

No. El stock se descuenta al confirmar la compra, no al agregar al carrito. Si el carrito reservara, un cliente que llena el carrito y se va dejaría esos muebles bloqueados para todos los demás. El carrito expresa una intención; el pedido es el compromiso.

### 18. ¿Cómo se paga?

Al confirmar la compra, el pedido queda registrado como pagado: nace en estado *Pagado* y con su fecha de pago. Cobrar el dinero le corresponde a una pasarela de pago (en Cuba, por ejemplo, Transfermóvil o EnZona), y conectarla queda fuera del objetivo de la asignatura, que no consiste en mover dinero real. Todo lo demás funciona igual que en una tienda en producción: el descuento de stock, el ciclo de vida del pedido, las cancelaciones y los reportes de ingresos. Para integrar la pasarela bastaría con confirmar el cobro antes de crear el pedido.

### 19. ¿Por qué el envío es gratis?

Es una decisión comercial: el precio del mueble incluye la entrega, así que el cliente ve desde el principio lo que va a pagar, sin sorpresas al final. El sistema está preparado para cobrarlo: cada pedido guarda el subtotal, el costo de envío y el total por separado (el total es la suma de los dos, y la base lo comprueba), y la regla está en un único sitio (`ShippingPolicy`). Cobrar el envío por provincia sería cambiar esa función.

### 20. ¿Por qué la entrega se estima en 7 días?

Es la promesa que la tienda le hace al cliente al comprar: tiempo para preparar el mueble, cargarlo y llevarlo. Se guarda en cada pedido como fecha estimada de entrega, y sirve para medir si se cumple: el reporte de Operación calcula qué porcentaje se entregó a tiempo y lista los pedidos atrasados. Una promesa que nadie mide no sirve de nada.

---

## Los pedidos

### 21. ¿Cuál es el ciclo de vida de un pedido y quién mueve cada paso?

*Pagado → En preparación → Enviado → Entregado* (RN15).

- El pedido nace *Pagado* al confirmar la compra.
- La tienda (un administrador) lo pasa a *En preparación*, luego a *Enviado* y por último a *Entregado*.
- En cualquier punto anterior al envío se puede pasar a *Cancelado*.

Cada paso guarda su fecha y queda en la auditoría con su autor. La API solo permite las transiciones válidas: no se puede enviar un pedido que no se preparó ni entregar uno cancelado.

### 22. ¿Por qué el cliente solo puede cancelar mientras el pedido está *Pagado*?

Porque hasta ese momento la tienda no ha invertido trabajo en él. Una vez *En preparación*, el mueble ya se está alistando, y la cancelación es una decisión de la tienda: el administrador sí puede cancelar en ese estado, por ejemplo si el cliente llama. Una vez *Enviado*, el mueble está en camino y ya no se trata de una cancelación, sino de una devolución, que es otro proceso.

### 23. ¿Por qué el cliente no puede marcar su pedido como entregado?

Porque la entrega la realiza y la certifica la tienda. Es quien sabe que el mueble llegó y a quién se le entregó, y también la responsable si no llegó. Si lo marcara el cliente, un pedido podría quedarse para siempre en *Enviado* porque nadie pulsó el botón, y los reportes de entregas a tiempo dependerían de la voluntad del cliente. Por eso marcar la entrega es exclusivo del administrador.

### 24. ¿Qué pasa con el stock y con los ingresos cuando se cancela un pedido?

El stock de cada producto vuelve al almacén en el mismo momento, así que esos muebles quedan otra vez a la venta. El pedido no se borra: queda *Cancelado*, con su fecha, su motivo y quién lo canceló. Deja de contar como venta en los reportes de ventas, productos y clientes, y pasa a contar en el de cancelaciones como ingreso perdido. Devolver el dinero sería trabajo de la pasarela de pago.

### 25. ¿Se pueden borrar pedidos o clientes?

- **Pedidos, nunca.** Un pedido es el registro de una venta. Si se borrara, los reportes cambiarían hacia atrás, la auditoría apuntaría a algo inexistente y quedaría un hueco en la numeración. Para lo que normalmente se querría borrar existe la cancelación.
- **Clientes, solo si nunca compraron:** una cuenta de prueba o un registro abandonado. Un cliente con pedidos, aunque estén cancelados, no se puede borrar: se **desactiva**, y ya no puede entrar, pero su historial sigue. Es la misma regla que con los productos: lo que tiene historia no se borra.

---

## Administración y control

### 26. ¿Qué registra la auditoría y para qué sirve?

Registra quién hizo qué y cuándo: cada cambio de productos, categorías, clientes y pedidos, incluidos los pedidos creados, además de cada exportación de reportes. De cada cambio guarda el valor anterior y el nuevo, por ejemplo el precio, de 120 a 135 (RN18). Sirve para responder preguntas como «¿quién cambió este precio?», «¿quién canceló este pedido?» o «¿quién se llevó el reporte de clientes?». En una tienda donde varias personas administran, sin auditoría nadie es responsable de nada. También alimenta el reporte de cancelaciones, que la usa para saber si canceló el cliente o la tienda.

### 27. ¿Qué pasa si dos administradores editan el mismo producto a la vez?

El segundo en guardar recibe un aviso: «Alguien más modificó este registro mientras lo editabas. Recarga para ver los cambios.» Sus cambios no se aplican a ciegas (RN19). Sin esta regla, el segundo borraría sin saberlo el trabajo del primero, por ejemplo, devolviendo un precio corregido a su valor viejo. Lo mismo vale para la ficha de un cliente.

### 28. ¿Por qué un administrador no puede desactivarse ni quitarse el rol a sí mismo, y por qué no se puede quitar el último?

Para que la tienda nunca se quede sin nadie al mando por un clic equivocado (RN17). Si el último administrador perdiera el rol, nadie podría volver a gestionar la tienda desde el panel. Por la misma razón, un administrador no puede borrarse ni desactivarse a sí mismo: esas acciones las hace otro administrador, con sus motivos y en la auditoría.

### 29. ¿Qué reportes tiene la tienda y para qué decisión sirve cada uno?

| Reporte | Pregunta que responde | Decisión que permite |
|---|---|---|
| Ventas | ¿Cuánto vendimos y cómo evoluciona? | Ver si la tienda crece y en qué días se vende más |
| Productos | ¿Qué se vende y qué no? | Qué reponer o destacar, qué rebajar o retirar |
| Operación | ¿Cumplimos con las entregas? | Dónde se atascan los pedidos y cuáles atender ya |
| Cancelaciones | ¿Cuánto perdemos y quién cancela? | Si el problema está en la oferta o en la operación |
| Inventario | ¿Qué tenemos y qué falta? | Qué reponer primero |
| Clientes | ¿Quién compra y desde dónde? | A quién cuidar y dónde concentrar las entregas |

Todos cuentan los días en la hora de La Habana y en dólares (RN20). Cada uno se puede exportar a PDF, para compartir, o su tabla a CSV, para trabajarla en una hoja de cálculo.

### 30. ¿Qué le falta a Furnistore para operar como una tienda real?

Lo sabemos y lo dejamos fuera del alcance a propósito:

- **Cobro real:** conectar una pasarela de pago antes de crear el pedido, y gestionar los reembolsos de las cancelaciones.
- **Devoluciones:** hoy un pedido enviado ya no se puede deshacer.
- **Costo de envío** por provincia o por volumen, en lugar de gratis.
- **Avisos al cliente** por correo en cada cambio de estado del pedido.
- **Validar el dígito de control del carnet**, cuando tengamos la fórmula oficial.
- **Facturas** o comprobantes en PDF para el cliente.

Nada de esto obliga a rehacer lo que existe: el ciclo de vida del pedido, las copias de precios, el registro del envío por separado y la auditoría ya son la base sobre la que se construiría.
