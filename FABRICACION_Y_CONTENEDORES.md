# Fabricación, inventario y contenedores

## Cómo empezar

1. Pica un tronco de árbol y recoge el tronco.
2. Abre el inventario con **E** o **I**. En el recetario, pulsa **Tablones de roble** para preparar los ingredientes y pulsa el resultado: un tronco produce cuatro tablones.
3. Prepara una **Mesa de trabajo** con cuatro tablones. Colócala en la barra y ponla en el mundo con un clic derecho.
4. Mira la mesa y pulsa clic derecho para abrir su rejilla de 3 × 3. Desde ahí puedes fabricar herramientas, cofres y hornos.
5. Coloca un cofre o un horno y ábrelo de la misma manera. Agáchate para colocar un bloque contra una estación sin abrirla.

La fabricación personal tiene una rejilla de 2 × 2. La mesa amplía la rejilla a 3 × 3, siguiendo la [guía oficial de fabricación de Minecraft](https://www.minecraft.net/en-us/article/how-craft).

## Controles

| Acción | Control |
| --- | --- |
| Abrir inventario | E / I, o el botón junto a la barra |
| Abrir mesa, horno o cofre | Clic derecho mirando el bloque |
| Cerrar la ventana | E / I / Esc / botón X |
| Tomar, colocar, combinar o intercambiar una pila | Clic izquierdo en una casilla |
| Mover entre casillas | Tomar y hacer clic en el destino, o arrastrar |
| Tomar media pila / colocar una unidad | Clic derecho en una casilla |
| Transferir rápidamente al cofre, horno o inventario | Shift + clic |
| Intercambiar una casilla con la barra | 1–9 mientras el cursor está sobre la casilla |
| Preparar una receta disponible | Clic en la receta del panel izquierdo |
| Recoger el resultado de fabricación | Clic izquierdo en la casilla de resultado |
| Fabricar hasta agotar los ingredientes puestos | Shift + clic en el resultado |
| Soltar una unidad del objeto seleccionado | Q |
| Atacar seres vivos | Clic izquierdo |
| Romper bloques | Mantener clic izquierdo |
| Colocar bloques | Clic derecho |
| Comer cuando tienes hambre | Mantener clic derecho con comida |
| Lanzar tridente | Mantener clic derecho hasta cargar y soltar |

El inventario contiene **36 casillas**, incluidas las nueve de la barra. Los materiales se apilan hasta 64; las herramientas ocupan una casilla cada una. Al cerrar la ventana, los ingredientes sin utilizar vuelven al inventario. Si no caben, aparecen en el suelo para recogerlos.

Las tres herramientas iniciales —espada de diamante, pico de diamante y tridente— siguen siendo permanentes, como se pidió anteriormente. Puedes cambiarlas de casilla dentro de tu inventario; no se consumen, no se rompen y no pueden depositarse en cofres, quemarse ni usarse como ingredientes. Las herramientas fabricadas sí tienen durabilidad.

## Recetas incluidas

Hay **23 recetas**, incluidas las dos variantes de antorchas con carbón o carbón vegetal. Debes respetar la disposición de los ingredientes; se aceptan patrones desplazados y reflejados.

| Resultado | Materiales y disposición |
| --- | --- |
| 4 tablones | 1 tronco |
| 4 palos | 2 tablones en vertical |
| Mesa de trabajo | 4 tablones formando un cuadrado de 2 × 2 |
| Cofre | 8 tablones alrededor de un centro vacío, en la mesa |
| Horno | 8 adoquines alrededor de un centro vacío, en la mesa |
| Espada | 2 materiales encima de 1 palo, en vertical |
| Pico | 3 materiales en la fila superior y 2 palos centrados debajo |
| Hacha | 3 materiales formando una L y 2 palos debajo del material interior |
| Pala | 1 material encima de 2 palos, en vertical |

Las cuatro herramientas se pueden fabricar en **madera, piedra, hierro y diamante**. Se usan tablones, adoquines, lingotes de hierro o diamantes, respectivamente. La cadena de fabricación usa materiales recogidos o procesados; el recetario solo prepara los ingredientes que tienes y no crea materiales gratis.

Las hachas son más rápidas con madera; las palas, con tierra y arena; los picos, con piedra. La piedra y los minerales solo sueltan su recurso si usas un pico apropiado: hierro requiere piedra o superior, y diamante requiere hierro o superior. La piedra picada produce adoquín, el carbón produce carbón y el hierro produce hierro en bruto. El combate conserva los cuatro golpes necesarios para matar a los animales y monstruos que ya tenía el proyecto.

## Hornos

Tienen tres casillas: ingrediente arriba, combustible abajo y resultado a la derecha. Cada objeto tarda **10 segundos**. El horno continúa trabajando al cerrar su ventana, y el frente se ilumina mientras arde. No admite objetos en la casilla del resultado.

| Entrada | Resultado |
| --- | --- |
| Hierro en bruto o mineral de hierro | Lingote de hierro |
| Tronco de roble | Carbón vegetal |
| Cerdo crudo | Chuleta cocinada |
| Cordero crudo | Cordero cocinado |
| Adoquín | Piedra |
| Arena | Vidrio |

Carbón y carbón vegetal arden 80 segundos; troncos, tablones, mesas y cofres, 15 segundos; palos, 5 segundos; herramientas de madera, 10 segundos. Recoger el resultado concede la experiencia acumulada de ese horno. Romperlo suelta su contenido.

## Cofres y guardado

Cada cofre tiene **27 casillas independientes**, una tapa animada y la textura del paquete suministrado. Conserva los objetos al cerrarlo. Un bloque encima impide abrirlo. Al romperlo, suelta tanto su contenido como el cofre.

Desde el menú de pausa, **Guardar mundo** conserva la distribución y durabilidad de los objetos, el contenido de cada cofre y el combustible y progreso de cada horno. **Cargar guardado** restaura esos datos. El formato es ahora versión 3 y puede importar los inventarios de las versiones anteriores. El archivo sigue siendo `mundo-rodolfo.json` dentro de `Application.persistentDataPath`.

## Texturas y alcance

Se reutilizaron del paquete que proporcionaste los paneles de inventario, mesa y horno; las texturas de los bloques funcionales; el atlas del cofre; y los iconos de ingredientes, comida cocinada y herramientas de los cuatro materiales. El cofre usa el mismo atlas para su modelo en el mundo, en la mano y su icono.

Este sistema cubre esas recetas, hornos y cofres simples. No incluye todavía cofres dobles, tolvas, encantamientos, armaduras ni todas las recetas de Minecraft. Se conserva el inventario al regenerar a Rodolfo y la carga del mundo continúa siendo manual.

La fabricación se comprobó inicialmente mediante compilación de scripts. Las nuevas funciones de inicio, equipo y misión se verificaron posteriormente en Play; consulta AVENTURA_DEL_DRAGON.md.
