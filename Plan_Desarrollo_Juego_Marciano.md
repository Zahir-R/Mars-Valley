# Plan de desarrollo del videojuego: Supervivencia en Marte
**Documento:** Plan de generación y desarrollo por etapas  
**Versión:** 0.1  
**Enfoque:** juego 2D top-down, pixel art, un jugador, supervivencia equilibrada y mundo procedural  
**Motor previsto:** Unreal Engine 4.27  
**Estado:** documento inicial de planificación

---

## 1. Objetivo del documento

Este documento organiza la creación del juego desde sus elementos más básicos hasta los primeros sistemas jugables. El orden prioriza construir una base pequeña y funcional antes de añadir sistemas complejos.

La primera meta no es crear todo Marte ni implementar todas las tecnologías. La primera meta es conseguir que el jugador pueda:

1. Explorar una pequeña zona marciana.
2. Encontrar y recoger materiales.
3. Guardar esos materiales en su inventario.
4. Fabricar herramientas sencillas.
5. Utilizar las herramientas para obtener nuevos recursos.
6. Fabricar componentes y objetos básicos.
7. Construir una instalación sencilla.
8. Guardar la partida y continuar después.

### 1.1 Decisiones de diseño

| Elemento | Decisión |
|---|---|
| Perspectiva | Top-down 2D |
| Estilo visual | Pixel art |
| Jugadores | Un jugador |
| Supervivencia | Equilibrada; la gestión importa, pero no debe castigar constantemente |
| Mundo | Procedural |
| Ambientación | Marte |
| Objetivo a largo plazo | Reparar la nave y decidir el futuro del astronauta |
| Conectividad | El juego debe funcionar sin conexión |

### 1.2 Principios de diseño

- **Progresión clara:** cada herramienta debe permitir conseguir algo nuevo o hacerlo de forma más eficiente.
- **Recursos con propósito:** evitar materiales que solo ocupen espacio y no tengan utilidad.
- **Supervivencia sin presión excesiva:** los errores deben dar tiempo para reaccionar.
- **Ciencia como inspiración:** usar materiales y condiciones marcianas reales como referencia, indicando cuándo una mecánica es una simplificación.
- **Contenido modular:** poder añadir minerales, recetas, estructuras y biomas sin reescribir todos los sistemas.
- **Mundo persistente:** los recursos extraídos y las construcciones del jugador deben conservarse al cargar la partida.

---

# PARTE I — DEFINICIÓN DE LOS ELEMENTOS DEL JUEGO

## 2. Clasificación de objetos

Antes de programar el inventario, conviene definir qué clases de objetos existirán. No todos los objetos deben comportarse igual.

### 2.1 Categorías

| Categoría | Descripción | Ejemplos |
|---|---|---|
| Recurso natural | Se encuentra directamente en el entorno | Regolito, roca, hielo |
| Material procesado | Se obtiene al transformar recursos | Lingote metálico, vidrio, fibra |
| Componente | Pieza fabricada que se utiliza en recetas mayores | Cable, circuito, tubería |
| Herramienta | Permite interactuar con el entorno de una forma especial | Pico, taladro manual |
| Consumible | Se utiliza y desaparece o reduce su cantidad | Comida, filtro, botiquín |
| Equipo | Objeto que proporciona una capacidad o protección | Tanque de oxígeno, traje |
| Semilla o cultivo | Elemento relacionado con la agricultura | Semillas de papa |
| Estructura | Objeto construible que permanece en el mundo | Almacén, panel solar |
| Objeto de misión | Elemento necesario para progresión o narrativa | Módulo de navegación |

### 2.2 Datos que debería tener cada objeto

Cada objeto debería definirse mediante datos, en lugar de programar un comportamiento distinto para cada recurso.

Campos recomendados:

| Campo | Función | Ejemplo |
|---|---|---|
| ID | Identificador único e interno | `resource_regolith` |
| Nombre | Nombre mostrado al jugador | Regolito |
| Descripción | Explica qué es y para qué sirve | Material suelto de la superficie marciana |
| Categoría | Tipo general de objeto | Recurso natural |
| Icono | Sprite del inventario | `T_Regolith_Icon` |
| Apilable | Si puede agruparse en una pila | Sí |
| Tamaño máximo de pila | Cantidad máxima por espacio | 99 |
| Peso | Si el juego utiliza límite de carga | 0.2 |
| Rareza | Frecuencia orientativa | Común |
| Etiquetas | Clasificación para recetas y filtros | `Mineral`, `Superficie` |
| Valor | Opcional; útil si existe comercio | Sin definir inicialmente |

**Recomendación:** usar identificadores estables y etiquetas. Por ejemplo, una receta puede pedir cualquier objeto con la etiqueta `Metal`, en lugar de depender de un nombre concreto.

---

## 3. Catálogo inicial de recursos

El primer prototipo debe tener pocos recursos. Es preferible implementar correctamente entre 6 y 10 materiales antes de crear decenas de minerales.

Las descripciones siguientes distinguen entre el material real y su función dentro del juego. Las recetas son propuestas de diseño, no afirmaciones de que el proceso industrial sea exactamente así en Marte.

### 3.1 Recursos que se recogen directamente

| Recurso | Descripción | Dónde se encuentra | Usos iniciales |
|---|---|---|---|
| Regolito | Capa de polvo, arena y fragmentos de roca que cubre la superficie marciana | Superficie | Ladrillos experimentales, relleno, extracción de minerales |
| Fragmentos de roca | Trozos de roca desprendidos de afloramientos y del terreno | Superficie y zonas rocosas | Trituración, muestras y fabricación básica |
| Basalto | Roca volcánica rica en minerales silicatados | Llanuras y afloramientos rocosos | Agregados, investigación y materiales de construcción |
| Sílice | Compuesto mineral presente en distintos materiales silicatados | Depósitos y rocas seleccionadas | Vidrio y componentes cerámicos |
| Hematita | Mineral de óxido de hierro | Formaciones y depósitos minerales | Fuente de hierro para procesos de fabricación |
| Hielo de agua | Agua congelada, potencialmente presente bajo la superficie en ciertas regiones | Zonas con depósitos de hielo | Agua, soporte vital y agricultura, después de procesarlo |
| Polvo marciano | Partículas finas que cubren el terreno | Casi toda la superficie | Investigación; uso limitado en fabricación |
| Muestra desconocida | Muestra geológica sin identificar | Puntos de interés | Investigación y desbloqueo de información |

**Notas de realismo:**
- El regolito no debe tratarse como tierra fértil. No es adecuado para cultivar directamente.
- La presencia de hielo depende de la región y de la profundidad. No debe aparecer de forma uniforme por todo el mapa.
- La hematita y otros minerales pueden servir como representación jugable de depósitos ricos en determinados elementos; el juego no necesita simular la composición química exacta de cada roca.
- El polvo marciano es importante para la ambientación y el mantenimiento, pero no es necesario convertirlo en un recurso útil para todas las recetas.

### 3.2 Materiales procesados iniciales

| Material | Se obtiene de | Descripción | Usos |
|---|---|---|---|
| Fragmentos triturados | Fragmentos de roca o basalto | Material de tamaño reducido | Mezclas de construcción y separación |
| Concentrado mineral | Roca procesada | Fracción enriquecida en minerales útiles | Metal y análisis |
| Lingote de hierro | Concentrado rico en hierro, tras varios procesos | Metal refinado para piezas resistentes | Herramientas y estructuras |
| Vidrio | Sílice procesada mediante un proceso de alta temperatura | Material transparente y frágil | Ventanas, lámparas y módulos |
| Agua purificada | Hielo extraído, fundido y tratado | Agua apta para usos definidos por el juego | Consumo, cultivos y procesos |
| Fibra aislante | Material fabricado a partir de insumos procesados | Aislante térmico para módulos | Hábitats y tuberías |

### 3.3 Componentes fabricados

| Componente | Descripción | Uso |
|---|---|---|
| Cable | Conductor eléctrico protegido | Conectar equipos y estructuras |
| Placa metálica | Lámina de metal procesado | Carcasas y construcción |
| Tornillos y fijaciones | Piezas pequeñas de unión | Herramientas y estructuras |
| Tubería | Conducto para transportar fluidos | Agua y sistemas de soporte vital |
| Filtro básico | Elemento que retiene partículas | Purificador y traje |
| Circuito simple | Componente electrónico elemental | Equipos y estaciones |
| Junta de sellado | Pieza que reduce fugas | Puertas, cúpulas y conexiones |
| Panel de vidrio | Lámina de vidrio preparada | Ventanas y módulos de cultivo |

### 3.4 Recursos que conviene reservar para etapas posteriores

No es necesario implementarlos en el primer prototipo:

- Aluminio y titanio como materiales especializados.
- Sulfatos y minerales hidratados.
- Arcillas marcianas.
- Percloratos, que deben tratarse como sustancias peligrosas y no como un recurso de uso cotidiano.
- Polímeros y materiales compuestos.
- Elementos raros para electrónica avanzada.
- Biomasa, algas y microorganismos para sistemas biológicos.

---

## 4. Herramientas iniciales

Las herramientas deben tener una progresión funcional. Una herramienta básica permite empezar; una mejor herramienta aumenta la velocidad, la eficiencia o el acceso a depósitos más duros.

| Herramienta | Función | Recurso que permite obtener | Evolución posible |
|---|---|---|---|
| Guantes de trabajo | Recoger objetos sueltos | Regolito, fragmentos pequeños, muestras | Guantes reforzados |
| Martillo geológico | Romper fragmentos pequeños y obtener muestras | Fragmentos de roca, muestras | Martillo reforzado |
| Pico manual | Extraer depósitos blandos o fracturados | Roca y minerales superficiales | Pico metálico |
| Pala plegable | Retirar material suelto | Regolito y depósitos poco profundos | Pala mecánica |
| Taladro manual | Perforar depósitos compactos | Muestras y recursos a poca profundidad | Taladro eléctrico |
| Recipiente de muestras | Guardar muestras científicas | Muestras geológicas | Contenedores especializados |

**Regla de diseño:** no permitir que el jugador recoja cualquier depósito con las manos. El tipo de nodo y la herramienta requerida deben determinar qué se puede extraer.

---

# PARTE II — MECÁNICAS BÁSICAS

## 5. Recolección

### 5.1 Tipos de objetos recolectables

1. **Objeto suelto:** se recoge directamente al interactuar.
2. **Nodo de recurso:** requiere una herramienta y varias acciones.
3. **Depósito:** contiene varias unidades y puede requerir una herramienta especializada.
4. **Muestra científica:** se recoge con un contenedor y se analiza posteriormente.
5. **Recurso procesable:** necesita una estación o una acción de procesamiento.

### 5.2 Flujo de recolección

1. El jugador se acerca al objeto o depósito.
2. El juego comprueba que está dentro del alcance de interacción.
3. El jugador pulsa el botón de interacción.
4. Si es un objeto suelto, se añade al inventario.
5. Si es un nodo, se comprueba la herramienta equipada.
6. Se aplica el daño o progreso de extracción.
7. Al completar la extracción, el nodo entrega sus recursos.
8. El nodo cambia de estado o desaparece.
9. El cambio se registra para que persista al guardar la partida.

### 5.3 Propiedades de un nodo

Cada nodo de recurso puede tener:

- ID del tipo de depósito.
- Cantidad restante.
- Dureza.
- Herramienta requerida.
- Tiempo de extracción.
- Tabla de posibles recompensas.
- Sprite y variaciones visuales.
- Estado: intacto, dañado, agotado.
- Posición en el mundo.
- Identificador único de instancia para guardar su estado.

Ejemplo conceptual:

| Nodo | Herramienta | Recompensa | Dificultad |
|---|---|---|---|
| Roca suelta | Ninguna o guantes | Fragmentos de roca | Muy baja |
| Afloramiento de basalto | Martillo o pico | Basalto | Baja |
| Veta de hematita | Pico | Fragmentos ricos en hierro | Media |
| Depósito compacto | Taladro | Muestra y material | Alta |
| Hielo superficial | Pala | Hielo contaminado | Media |

### 5.4 Recolección sin herramienta

El jugador debe poder comenzar sin fabricar nada.

Acciones iniciales:
- Recoger fragmentos pequeños de roca.
- Recoger regolito con las manos o con una herramienta improvisada.
- Examinar muestras.
- Recuperar piezas sueltas de los restos de la nave.

Esto evita que el jugador se encuentre bloqueado al empezar.

---

## 6. Inventario

### 6.1 Inventario inicial

Para el prototipo:

- Inventario por espacios.
- Objetos apilables.
- Cantidad visible en cada espacio.
- Selección de objeto.
- Descripción al seleccionar.
- Acción para utilizar o equipar.
- Acción para dividir o descartar pilas, si resulta necesaria.

No implementar todavía peso, deterioro de objetos ni cientos de espacios.

### 6.2 Reglas

- Los objetos iguales se agrupan si son apilables.
- Si una pila llega al máximo, el excedente ocupa otro espacio.
- Si no hay espacio, la recolección no se completa o el objeto queda en el suelo.
- Los objetos importantes de misión pueden tener reglas especiales.
- El inventario debe guardar el ID del objeto y la cantidad, no solo el nombre mostrado.

### 6.3 Interfaz inicial

La interfaz debe mostrar:
- Espacios del inventario.
- Icono y cantidad.
- Objeto seleccionado.
- Descripción.
- Barra rápida para herramientas.

---

## 7. Crafteo y fabricación

### 7.1 Concepto

El crafteo transforma recursos en objetos. Al principio se realizará desde una estación muy sencilla; más adelante se añadirán máquinas especializadas.

Cada receta debería tener:

- ID.
- Nombre.
- Descripción.
- Ingredientes y cantidades.
- Resultado y cantidad.
- Estación requerida.
- Tiempo de fabricación, si aplica.
- Nivel tecnológico o investigación requerida.
- Desbloqueo inicial o progresivo.

### 7.2 Primeras recetas propuestas

Las cantidades son valores iniciales de balance y deberán probarse durante el prototipo.

| Resultado | Ingredientes | Estación | Propósito |
|---|---|---|---|
| Martillo improvisado | 3 fragmentos de roca + 2 fijaciones recuperadas | Fabricación manual | Extraer rocas |
| Pala improvisada | 4 fragmentos de roca + 2 fijaciones | Fabricación manual | Recoger regolito |
| Pico básico | 3 fragmentos de roca + 2 fijaciones + 1 mango | Fabricación manual | Extraer depósitos blandos |
| Contenedor de muestras | 2 placas recuperadas | Fabricación manual | Guardar muestras |
| Fragmentos triturados | 3 fragmentos de roca | Trituradora | Material de procesamiento |
| Concentrado mineral | 5 fragmentos triturados | Separador | Preparar refinado |
| Placa metálica | 3 unidades de lingote de hierro | Horno y banco | Fabricar componentes |
| Cable | 1 placa metálica + material conductor | Banco de fabricación | Conexiones eléctricas |
| Tubería | 2 placas metálicas | Banco de fabricación | Transporte de agua |
| Junta de sellado | Material polimérico o aislante | Banco de fabricación | Sellar conexiones |

**Importante:** el jugador no debería poder fabricar un lingote de hierro directamente con una roca. Debe existir una cadena de procesamiento, aunque en el prototipo se puede simplificar.

### 7.3 Estaciones de fabricación

| Estación | Función inicial | Desbloqueo |
|---|---|---|
| Fabricación manual | Herramientas y objetos sencillos | Disponible al comenzar |
| Trituradora | Reduce rocas a fragmentos | Fabricar o reparar |
| Separador mineral | Separa fracciones útiles | Investigar procesamiento |
| Horno | Procesa materiales a alta temperatura | Reparar sistema térmico |
| Banco de fabricación | Crea componentes y herramientas | Construcción inicial |
| Purificador | Trata agua obtenida del hielo | Investigación de soporte vital |

### 7.4 Reglas de fabricación

1. Mostrar solo recetas desbloqueadas o indicar claramente las bloqueadas.
2. Mostrar los ingredientes requeridos y los disponibles.
3. Deshabilitar la fabricación si faltan materiales.
4. Consumir ingredientes solo cuando se inicia correctamente la fabricación.
5. Entregar el resultado al inventario o a la salida de la estación.
6. Si hay tiempo de fabricación, permitir que el jugador realice otras actividades.
7. Evitar que el jugador pierda materiales por cerrar accidentalmente una interfaz.

---

# PARTE III — PRIMERA PROGRESIÓN JUGABLE

## 8. Inicio de partida

### 8.1 Situación inicial

El jugador despierta cerca de los restos de su nave. El accidente ha destruido o inutilizado parte del equipo, pero quedan algunos objetos recuperables.

El área inicial debe ser pequeña y segura. No se debe exponer al jugador a todos los sistemas de supervivencia desde el primer minuto.

### 8.2 Objetos iniciales sugeridos

| Objeto | Cantidad | Motivo |
|---|---:|---|
| Traje espacial dañado | 1 | Permite salir al exterior, con limitaciones |
| Herramienta multiuso | 1 | Interacción y reparación sencilla |
| Recipiente de muestras | 1 | Recolección científica |
| Fragmentos metálicos recuperados | 4 | Primeras recetas |
| Batería portátil | 1 | Alimentar un equipo pequeño |
| Raciones de emergencia | 2 | Introducir alimentación sin presión |
| Kit de reparación incompleto | 1 | Primer objetivo de progresión |

Las cantidades son orientativas. El inventario inicial debe permitir fabricar una primera herramienta sin obligar al jugador a buscar un recurso imposible de obtener.

### 8.3 Primeras tareas guiadas

1. Examinar los restos de la nave.
2. Recoger fragmentos de roca cercanos.
3. Recuperar piezas metálicas.
4. Fabricar un martillo improvisado.
5. Romper un afloramiento pequeño.
6. Recoger más materiales.
7. Fabricar una pala.
8. Retirar regolito de una zona marcada.
9. Instalar un pequeño módulo de almacenamiento.
10. Reparar un equipo básico de energía.

Estas tareas enseñan las mecánicas sin depender de largos tutoriales de texto.

---

## 9. Primeras estructuras

Las primeras estructuras deben resolver necesidades inmediatas y preparar la expansión.

| Estructura | Función | Requisitos de diseño |
|---|---|---|
| Almacén básico | Guardar recursos fuera del inventario | Interfaz de depósito |
| Banco de fabricación | Crear herramientas y componentes | Recetas y consumo de materiales |
| Panel solar pequeño | Generar energía durante el día | Orientación o ubicación simplificada |
| Batería | Almacenar energía | Capacidad y carga |
| Lámpara | Iluminar el refugio | Consumo eléctrico opcional |
| Módulo habitable | Proporcionar un espacio protegido | Integridad y sellado |
| Purificador sencillo | Convertir agua obtenida del hielo en agua utilizable | Energía y filtros |

No es necesario que todas estén disponibles desde el primer día. El primer prototipo puede incluir solamente el almacén y el banco de fabricación.

---

## 10. Construcción sobre cuadrícula

### 10.1 Reglas iniciales

- El mundo se divide en celdas.
- Las estructuras ocupan una o más celdas.
- El jugador selecciona una estructura desde una interfaz.
- Se muestra una previsualización antes de colocarla.
- La colocación comprueba que el espacio esté libre.
- La estructura consume los materiales necesarios.
- La estructura conserva su posición y estado al guardar.

### 10.2 Validaciones

Una estructura no puede colocarse si:
- Está fuera de la zona permitida.
- Ocupa una celda bloqueada.
- Se superpone con otra estructura.
- Requiere una superficie que no existe.
- No hay suficientes materiales.

En esta primera etapa no es necesario simular presión, tuberías ni redes complejas.

---

# PARTE IV — SUPERVIVENCIA EQUILIBRADA

## 11. Necesidades del astronauta

La supervivencia debe aportar decisiones, no convertirse en una tarea repetitiva que interrumpa cada pocos minutos.

### 11.1 Sistemas recomendados

| Sistema | Función | Comportamiento recomendado |
|---|---|---|
| Oxígeno | Permite permanecer fuera del refugio | Limita expediciones, con avisos tempranos |
| Hidratación | Representa el consumo de agua | Desciende lentamente |
| Alimentación | Representa energía nutricional | Descenso lento; penalizaciones progresivas |
| Temperatura | Representa exposición térmica | Depende del entorno y del equipo |
| Energía del traje | Alimenta sistemas del traje | Se consume durante expediciones |
| Salud | Estado general del personaje | Se afecta por peligros y negligencia prolongada |

### 11.2 Filosofía de penalizaciones

- Avisar antes de alcanzar un estado peligroso.
- Dar tiempo suficiente para regresar al refugio.
- No matar instantáneamente por una necesidad agotada.
- Permitir ajustar la dificultad.
- Evitar que todas las necesidades bajen a la vez de forma agresiva.
- Hacer que el refugio sea un lugar seguro para descansar y organizarse.

### 11.3 Primera implementación

Para el prototipo, implementar solo:
1. Oxígeno durante expediciones.
2. Agua como recurso consumible.
3. Energía del traje.

Dejar hambre, temperatura avanzada, radiación y enfermedades para etapas posteriores.

---

# PARTE V — AGRICULTURA Y AUTOSUFICIENCIA

## 12. Primera cúpula agrícola

La agricultura debe aparecer después de que el jugador haya conseguido un refugio y un suministro básico de energía y agua.

### 12.1 Concepto

Las plantas no se cultivan directamente en el suelo marciano. Se utilizan bandejas o módulos hidropónicos dentro de un espacio presurizado y controlado.

### 12.2 Elementos iniciales

| Elemento | Función |
|---|---|
| Cúpula o módulo presurizado | Mantener un ambiente controlado |
| Bandeja hidropónica | Alojar las plantas |
| Depósito de agua | Suministrar agua |
| Nutrientes | Aportar elementos necesarios para el crecimiento |
| Lámpara de cultivo | Proporcionar iluminación controlada |
| Sensor ambiental | Mostrar temperatura y humedad |
| Semillas | Iniciar un cultivo |

### 12.3 Primer cultivo

Comenzar con un solo cultivo para validar el sistema. Una opción inicial es la lechuga por su ciclo de crecimiento relativamente corto en condiciones controladas. Posteriormente se pueden añadir papa, tomate, trigo, zanahoria y otras plantas.

Cada cultivo debería definir:
- Tiempo de crecimiento.
- Agua requerida.
- Nutrientes requeridos.
- Iluminación necesaria.
- Etapas visuales de crecimiento.
- Cantidad de cosecha.
- Posibilidad de recuperar semillas.

---

# PARTE VI — GENERACIÓN PROCEDURAL DEL MUNDO

## 13. Principios del mapa procedural

El mundo debe sentirse marciano y coherente, pero no necesita reproducir cada metro de una región real.

Se recomienda un sistema híbrido:

1. Datos geográficos reales como referencia de elevación y geología.
2. Generación procedural para crear variaciones jugables.
3. Distribución por reglas para colocar recursos y puntos de interés.
4. Persistencia para conservar los cambios del jugador.

### 13.1 Capas del mapa

| Capa | Contenido | Uso |
|---|---|---|
| Elevación | Alturas y depresiones | Pendientes, cráteres y zonas bajas |
| Geología | Tipos de terreno y materiales | Probabilidad de recursos |
| Superficie | Polvo, roca, arena y regolito | Apariencia y movimiento |
| Interés | Restos, anomalías y estructuras | Exploración |
| Recursos | Depósitos minerales y hielo | Recolección |
| Navegación | Caminos, obstáculos y zonas transitables | Movimiento del jugador |

### 13.2 Escala recomendada

- **Región:** define el contexto geográfico general.
- **Zona de exploración:** contiene biomas y puntos de interés.
- **Sector:** área que el jugador recorre durante una expedición.
- **Celda o tile:** unidad de construcción y navegación.

### 13.3 Reglas de distribución iniciales

- Regolito: muy común en zonas abiertas.
- Fragmentos de roca: comunes, con densidad variable.
- Basalto: más frecuente en terrenos volcánicos o afloramientos apropiados.
- Hematita: depósitos poco comunes, asociados a zonas geológicas seleccionadas.
- Hielo: limitado a regiones y condiciones donde su presencia sea plausible.
- Restos tecnológicos: puntos de interés poco frecuentes.
- Muestras científicas: cerca de formaciones geológicas o anomalías.

No distribuir todos los recursos con ruido aleatorio independiente. La geología debe influir en las probabilidades.

### 13.4 Semilla y persistencia

Cada partida debe tener una semilla que permita reproducir la generación inicial.

Guardar por separado:
- Semilla del mundo.
- Región y parámetros de generación.
- Depósitos agotados o modificados.
- Objetos recogidos.
- Estructuras construidas.
- Cambios de terreno.
- Estado de estaciones y máquinas.

No guardar una copia completa del mapa si puede regenerarse a partir de la semilla y de una lista de modificaciones.

---

# PARTE VII — PLAN DE IMPLEMENTACIÓN POR PASOS

## 14. Fases de desarrollo

Cada fase debe terminar con una versión jugable y comprobable. No avanzar solo porque el código compile: el sistema debe funcionar dentro del juego.

### Fase 0 — Preparación del proyecto

**Objetivo:** crear una base técnica ordenada.

Tareas:
- Crear el proyecto 2D en Unreal Engine 4.27.
- Configurar resolución y cámara top-down.
- Definir tamaño de tile y escala de sprites.
- Crear carpetas para personajes, entorno, objetos, interfaz, datos y audio.
- Definir convención de nombres.
- Crear un mapa de prueba.

**Resultado:** personaje visible en una escena vacía y proyecto organizado.

### Fase 1 — Movimiento e interacción

**Objetivo:** controlar al astronauta y examinar el entorno.

Tareas:
- Movimiento en cuatro u ocho direcciones.
- Colisiones con objetos y paredes.
- Cámara que siga al jugador.
- Sistema de interacción por proximidad.
- Indicador visual del objeto enfocado.
- Mensaje contextual de interacción.

**Prueba de aceptación:** el jugador puede caminar por una pequeña zona, acercarse a un objeto y activar su interacción.

### Fase 2 — Objetos y datos

**Objetivo:** definir objetos sin crear una clase exclusiva para cada material.

Tareas:
- Crear una estructura de datos para objetos.
- Crear un catálogo inicial de recursos.
- Asignar IDs, nombres, descripciones, iconos y categorías.
- Definir apilamiento.
- Crear sprites provisionales.
- Crear una forma de consultar un objeto por ID.

**Prueba de aceptación:** el juego puede reconocer y mostrar distintos objetos a partir de sus datos.

### Fase 3 — Recolección

**Objetivo:** obtener recursos del mundo.

Tareas:
- Crear objetos sueltos recogibles.
- Crear nodos de recursos.
- Definir herramientas requeridas.
- Implementar progreso de extracción.
- Entregar recompensas.
- Actualizar visualmente los nodos.
- Registrar nodos agotados.

**Contenido mínimo:** regolito, fragmentos de roca y basalto.

**Prueba de aceptación:** el jugador recoge objetos sueltos y extrae recursos de al menos un nodo.

### Fase 4 — Inventario

**Objetivo:** almacenar y gestionar los recursos.

Tareas:
- Crear espacios de inventario.
- Apilar objetos.
- Mostrar cantidades.
- Seleccionar objetos.
- Equipar herramientas.
- Gestionar inventario lleno.
- Guardar y cargar contenido.

**Prueba de aceptación:** los recursos recogidos aparecen en el inventario y permanecen después de guardar y cargar.

### Fase 5 — Herramientas y crafteo básico

**Objetivo:** permitir que los recursos se conviertan en herramientas.

Tareas:
- Crear sistema de recetas basado en datos.
- Crear interfaz de fabricación.
- Comprobar ingredientes.
- Consumir materiales.
- Crear resultados.
- Fabricar martillo, pala y pico básicos.
- Comprobar herramientas al extraer recursos.

**Prueba de aceptación:** el jugador recoge materiales, fabrica una herramienta y la utiliza para obtener un recurso que antes no podía conseguir.

### Fase 6 — Primeras estaciones

**Objetivo:** introducir procesamiento de materiales.

Tareas:
- Crear banco de fabricación.
- Crear trituradora sencilla.
- Procesar fragmentos de roca.
- Crear concentrado mineral como recurso intermedio.
- Añadir tiempos de proceso solo si mejoran el ritmo de juego.
- Guardar el estado de las estaciones.

**Prueba de aceptación:** el jugador transforma un recurso natural en un material procesado.

### Fase 7 — Construcción y almacenamiento

**Objetivo:** permitir que el jugador establezca una pequeña base.

Tareas:
- Crear modo de construcción.
- Mostrar previsualización.
- Validar celdas libres.
- Consumir materiales al colocar.
- Crear almacén.
- Crear banco de fabricación como estructura.
- Guardar posiciones y estados.

**Prueba de aceptación:** el jugador construye un almacén y una estación, deposita recursos y continúa jugando después de cargar la partida.

### Fase 8 — Energía y refugio

**Objetivo:** hacer que la base tenga una función de supervivencia.

Tareas:
- Crear panel solar básico.
- Crear batería.
- Definir producción y almacenamiento de energía.
- Crear un módulo habitable.
- Permitir recuperar energía o descansar dentro del refugio.
- Añadir una interfaz sencilla de energía.

**Prueba de aceptación:** una estructura produce energía y otra puede consumirla.

### Fase 9 — Primer ciclo de supervivencia

**Objetivo:** introducir una necesidad que afecte las expediciones.

Tareas:
- Implementar oxígeno del traje.
- Mostrar nivel y avisos.
- Definir consumo por tiempo fuera del refugio.
- Permitir reponer oxígeno.
- Añadir agua como recurso consumible.
- Ajustar valores para que la exploración no resulte estresante.

**Prueba de aceptación:** el jugador puede realizar una expedición, recibir avisos y regresar a tiempo.

### Fase 10 — Agua y agricultura

**Objetivo:** conseguir una fuente renovable de recursos.

Tareas:
- Crear depósitos de hielo.
- Extraer hielo contaminado.
- Fundir y tratar el agua.
- Crear una bandeja hidropónica.
- Añadir semillas y crecimiento por etapas.
- Consumir agua y nutrientes.
- Cosechar.

**Prueba de aceptación:** el jugador obtiene agua y completa un ciclo de cultivo.

### Fase 11 — Generación procedural

**Objetivo:** generar zonas distintas y reproducibles.

Tareas:
- Definir semilla.
- Generar un mapa pequeño por sectores.
- Crear reglas de terreno.
- Distribuir nodos según geología.
- Añadir obstáculos y puntos de interés.
- Garantizar una zona inicial jugable.
- Guardar cambios del mundo.

**Prueba de aceptación:** dos semillas producen mapas distintos y una misma semilla reproduce el mismo mapa inicial.

### Fase 12 — Expansión del contenido

**Objetivo:** ampliar el juego sin romper los sistemas anteriores.

Añadir progresivamente:
- Más minerales y materiales.
- Nuevas herramientas.
- Máquinas especializadas.
- Cúpulas y módulos mayores.
- Más cultivos.
- Investigación científica.
- Automatización.
- Tormentas de polvo.
- Cuevas y depósitos subterráneos.
- Restos de misiones y narrativa ambiental.
- Sistemas necesarios para reparar la nave.

---

## 15. Alcance del primer prototipo

El primer prototipo debe ser pequeño y terminarse antes de ampliar el contenido.

### Contenido obligatorio

- Un mapa pequeño.
- Un astronauta controlable.
- Movimiento top-down.
- Tres recursos: regolito, fragmentos de roca y basalto.
- Dos nodos de recolección diferentes.
- Inventario por espacios.
- Tres herramientas básicas: martillo, pala y pico.
- Crafteo manual.
- Una estación de fabricación.
- Una trituradora sencilla.
- Un almacén.
- Una estructura construible.
- Guardado y carga.

### Contenido que se deja fuera del primer prototipo

- Mapa de Marte a escala regional.
- Datos NASA integrados en tiempo de ejecución.
- Agricultura completa.
- Simulación detallada de oxígeno y presión.
- Redes complejas de energía y tuberías.
- Cuevas generadas proceduralmente.
- Multijugador.
- Comercio.
- Inteligencia artificial avanzada.

**Criterio para cerrar el prototipo:** el jugador debe poder comenzar con pocos objetos, recoger recursos, fabricar una herramienta, utilizarla, procesar materiales, construir una pequeña instalación y conservar su progreso al cargar la partida.

---

# PARTE VIII — ORGANIZACIÓN TÉCNICA SUGERIDA

## 16. Sistemas principales

La arquitectura debe separar datos y comportamiento. Un objeto no debería necesitar una clase C++ exclusiva solo por ser un mineral distinto.

| Sistema | Responsabilidad |
|---|---|
| `ItemData` | Datos de cada objeto |
| `InventoryComponent` | Espacios, cantidades y operaciones de inventario |
| `ResourceNode` | Estado y extracción de un depósito |
| `GatheringComponent` | Interacción de recolección del jugador |
| `ToolData` | Datos y capacidades de herramientas |
| `RecipeData` | Ingredientes y resultados de recetas |
| `CraftingComponent` | Validación y ejecución de recetas |
| `CraftingStation` | Actor de una estación del mundo |
| `BuildingSystem` | Previsualización y colocación de estructuras |
| `WorldGenerator` | Generación de sectores y distribución de recursos |
| `SaveGame` | Persistencia de la partida |

Los nombres son orientativos y pueden adaptarse al estilo del proyecto.

## 17. Estructuras de datos conceptuales

### 17.1 Objeto

```text
ItemDefinition
- ItemID
- DisplayName
- Description
- Category
- Icon
- bStackable
- MaxStack
- Tags
```

### 17.2 Pila del inventario

```text
InventorySlot
- ItemID
- Quantity
```

### 17.3 Receta

```text
RecipeDefinition
- RecipeID
- DisplayName
- Ingredients[]
- ResultItemID
- ResultQuantity
- RequiredStation
- CraftingTime
- RequiredResearch
```

### 17.4 Nodo de recurso

```text
ResourceNodeState
- InstanceID
- ResourceType
- Position
- RemainingAmount
- CurrentDamage
- bDepleted
```

Estas estructuras son una guía de diseño, no código final. En Unreal Engine pueden implementarse con `UDataAsset`, `UDataTable`, `USTRUCT` y componentes, según convenga.

## 18. Organización de carpetas sugerida

```text
Content/
├── Core/
│   ├── GameMode/
│   ├── GameInstance/
│   └── Save/
├── Characters/
│   └── Astronaut/
├── World/
│   ├── Tiles/
│   ├── Generation/
│   ├── Biomes/
│   └── ResourceNodes/
├── Items/
│   ├── Data/
│   ├── Icons/
│   └── Tools/
├── Inventory/
├── Crafting/
│   ├── Recipes/
│   └── Stations/
├── Building/
├── Survival/
├── Farming/
├── UI/
├── Audio/
└── Art/
```

---

# PARTE IX — BALANCE Y PRUEBAS

## 19. Pruebas que deben realizarse desde el principio

| Prueba | Qué se comprueba |
|---|---|
| Recoger un objeto | El inventario recibe la cantidad correcta |
| Inventario lleno | No se pierden objetos silenciosamente |
| Extraer un nodo | La herramienta y la recompensa son correctas |
| Agotar un depósito | El nodo queda agotado |
| Fabricar una herramienta | Se consumen los ingredientes correctos |
| Fabricación inválida | No se consumen materiales si faltan ingredientes |
| Construir una estructura | No se permiten superposiciones |
| Guardar y cargar | Se conserva inventario y mundo |
| Generar con una semilla | El mapa es reproducible |
| Generar con otra semilla | El mapa cambia sin perder jugabilidad |

## 20. Reglas iniciales de balance

- El jugador debe encontrar recursos básicos cerca del punto de inicio.
- La primera herramienta debe fabricarse en pocos minutos.
- Las primeras recetas deben enseñar una cadena de materiales sencilla.
- No introducir demasiados recursos con nombres parecidos.
- No exigir una máquina avanzada para fabricar el objeto que permite construir esa misma máquina.
- Cada material nuevo debe tener al menos un uso inmediato o una razón clara para conservarse.
- Las expediciones deben durar lo suficiente para explorar y recolectar.
- El jugador debe poder organizar su base sin que las necesidades bajen demasiado rápido.

---

# 21. Lista de tareas inmediata

Esta es la secuencia concreta recomendada para comenzar el proyecto:

- [ ] Crear el proyecto y el mapa de prueba.
- [ ] Configurar cámara top-down y movimiento.
- [ ] Crear un objeto de prueba recogible.
- [ ] Definir la estructura de datos de los objetos.
- [ ] Crear los datos de regolito, fragmentos de roca y basalto.
- [ ] Implementar interacción por proximidad.
- [ ] Implementar recolección de objetos sueltos.
- [ ] Crear un nodo de roca extraíble.
- [ ] Implementar herramienta requerida y progreso de extracción.
- [ ] Crear inventario por espacios.
- [ ] Mostrar iconos y cantidades.
- [ ] Crear recetas de martillo, pala y pico.
- [ ] Implementar fabricación manual.
- [ ] Probar el ciclo completo de recolección y crafteo.
- [ ] Añadir guardado y carga.

## 22. Decisión de alcance

No comenzar por el mapa procedural completo ni por la simulación científica avanzada. Primero debe existir un ciclo de juego divertido y estable en un mapa pequeño diseñado a mano.

Una vez que el ciclo de recolección, inventario y fabricación funcione, se puede reemplazar el mapa de prueba por sectores generados proceduralmente. Así, los sistemas de objetos y herramientas no dependerán de cómo se genere el terreno.

**Primer hito real del proyecto:** un astronauta recoge fragmentos de roca, fabrica un martillo, extrae basalto y utiliza ese material para fabricar un objeto nuevo. Ese pequeño ciclo será la base sobre la que crecerá el resto del juego.
