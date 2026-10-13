# Inicio, equipo y misión del Dragón del End

## Empezar

Al entrar en Play aparece el menú **RODOLFO · AVENTURA VOXEL**. Pulsa **INICIAR** o Enter: se muestra la carga de dos segundos y después comienza el juego en primera persona.

El equipo inicial contiene:

- Espada de diamante, pico de diamante y tridente permanentes.
- **Arco de fuego e infinidad**, permanente y sin consumo de flechas.
- **64 antorchas**.
- **16 manzanas doradas encantadas**.
- Casco, peto, pantalones y botas de **diamante**, ya equipados.

## Antorchas

Selecciona las antorchas en la barra y pulsa **clic derecho** mirando el suelo o una pared. Emiten luz cálida real, con un parpadeo suave. No ocultan ni eliminan la cara del bloque de apoyo.

Puedes picarlas y recogerlas. Si destruyes su apoyo, se desprenden y aparecen como objetos recogibles. Su posición, orientación y dimensión quedan incluidas en el guardado. No se pueden colocar bajo el techo ni sobre criaturas.

También se fabrican: **carbón o carbón vegetal encima de un palo → 4 antorchas**. Las luces cercanas se activan por distancia y no proyectan sombras para evitar añadir un coste elevado a la edición del mundo.

## Armadura y manzanas

La armadura aparece sobre Rodolfo en tercera persona y su protección se indica sobre los corazones. Las cuatro casillas de armadura están en la columna izquierda del inventario personal. Puedes mover las piezas al inventario o volver a equiparlas; cada casilla acepta su pieza correspondiente.

Las piezas tienen durabilidad y reducen el daño físico. El aliento del dragón, las caídas, el hambre y el ahogo no usan esa reducción de armadura.

Mantén **clic derecho** con una manzana encantada para comerla, incluso si tienes el hambre llena. Concede:

- **8 corazones de absorción** durante hasta 120 segundos, mostrados en dorado.
- Regeneración durante 20 segundos.
- Resistencia durante 300 segundos.

Los efectos se renuevan al comer otra manzana. La comida y los efectos restantes se guardan junto con la partida.

## Arco

Mantén **clic derecho** para tensarlo y apunta con el ratón. Suelta el botón para disparar. Cargarlo completamente aumenta el alcance y el daño; las flechas tienen trayectoria y gravedad, por lo que debes compensar la caída en tiros largos.

Las flechas llevan un rastro de fuego y pueden incendiar zombis, creepers y animales. El dragón recibe el impacto físico de la flecha; no recibe daño adicional por quemadura. El arco inicial no se gasta ni requiere una pila de flechas. Abrir el inventario, cambiar de objeto o entrar en una interfaz cancela la carga.

El arco tiene una postura independiente del pico: aparece a la derecha y su flecha apunta hacia dentro, hacia la parte superior izquierda de la pantalla. Al tensarlo se acerca ligeramente al centro. En tercera persona se orienta hacia delante en la mano derecha. Cambiar al pico restaura su posición habitual.

## Aceptar la misión

Pulsa **M**, o el botón **Misiones [M]** del inventario o de la interfaz, y elige **Aceptar misión**.

Se crea una arena separada del End con una isla, fuente central y **10 torres de obsidiana con cristales**. El mundo normal y sus criaturas quedan suspendidos mientras combates. La cinemática dura seis segundos y puedes omitirla con Espacio o Esc.

La isla usa una malla estática para la misión; puedes colocar antorchas en ella, pero no editar sus torres y suelo con el sistema de bloques del mundo normal.

### Ambiente del End

La arena tiene cielo oscuro, niebla violeta, lluvia inclinada por el viento y relámpagos sobre las torres, con destellos de iluminación y truenos. El sol y la luna del mundo normal quedan ocultos. La tormenta también se anima durante la cinemática; se detiene al pausar el combate y desaparece al salir del End.

Los relámpagos son ambientales. La lluvia usa un único emisor con un máximo de 700 partículas, sin colisiones ni sombras, y el rayo reutiliza su geometría. Estos efectos no reconstruyen sectores ni alteran bloques. Al regresar se recuperan el cielo, la iluminación y la niebla del mundo normal.

## Combate

- El dragón usa el modelo que proporcionaste en `minecraft-ender-dragon/`, incluida la textura interna de su GLB. Se añadieron vuelo y aleteo por código porque el archivo no incluía animaciones.
- Tiene **200 puntos de vida**, visibles en una barra morada que se actualiza al recibir daño o regenerarse.
- Los cristales activos lo regeneran. El cristal curador más cercano muestra el rayo de energía. Dispara a los cristales para destruirlos; explotan y pueden herirte si estás cerca.
- El dragón alterna vuelo, embestidas y descensos a la fuente central. Lanza aliento violeta que deja zonas peligrosas en el suelo.
- Su cuerpo mira en la dirección de su desplazamiento real. Suaviza la velocidad en los giros, inclina el cuerpo al virar y frena al aterrizar. Después de una embestida continúa volando y asciende; al terminar de posarse gira hacia Rodolfo.
- La cabeza recibe más daño. Puedes atacarlo con flechas, tridente, espada o con otros objetos a corta distancia.
- **Cuando está posado resiste los proyectiles:** acércate y usa la espada. La interfaz indica esa fase.
- El aliento y las embestidas pueden matar a Rodolfo. Caer fuera de la isla también es mortal.

Las fases, cristales y protección al posarse se basan en la [descripción oficial del Dragón del End](https://www.minecraft.net/en-us/article/ender-dragon). Los tiempos, daño y movimiento son una adaptación para este proyecto; no se reproduce exactamente toda la simulación del juego original. Los beneficios de las manzanas se basan en la [definición oficial de la manzana encantada](https://learn.microsoft.com/en-us/minecraft/creator/reference/source/vanillabehaviorpack_snippets/items/appleenchanted?view=minecraft-bedrock-stable).

## Muerte, victoria y guardado

Si mueres, **Regenerar** te devuelve al mundo normal con vida. Puedes retomar la misión desde M; conserva los cristales destruidos y el daño del dragón.

Al derrotarlo se reproduce su muerte y aparece **¡VICTORIA!**, con una recompensa de **1200 de experiencia** y un botón para continuar en el mundo. La misión queda marcada como completada.

Puedes regresar temporalmente desde el menú de misión durante el combate. La partida ahora usa el formato de guardado **versión 3**, con equipo, efectos, dimensión, vida del dragón y cristales. Sigue admitiendo los guardados anteriores y les añade el equipo nuevo; si el inventario está lleno, los materiales que no caben se dejan para recogerlos, sin descartar las pilas previas.

## Verificación

La ejecución de estos ajustes terminó correctamente: **69 comprobaciones pasaron**, sin errores ni excepciones de Unity. También se revisaron visualmente las capturas del arco en primera persona y de la tormenta del End, además de las comprobaciones anteriores del equipo y del combate.

Las comprobaciones automatizadas se ejecutan en Play con Unity 2023.2.20f1 sobre una copia aislada del proyecto. Los guardados de prueba se escriben en esa carpeta de verificación, no en el archivo de partida del usuario. Cubren inicio, carga de dos segundos, equipo, efectos de comida, luz y apoyo de antorchas, cinemática, vuelo, impactos de flechas, cristales, regeneración, guardado y carga, muerte, reaparición y victoria.

Se verifican también la postura del arco y del pico, la lluvia durante la cinemática, los relámpagos, la orientación del dragón durante doce fotogramas y la desactivación de la tormenta al regresar. El informe de estos ajustes está en `.codex-backups/ajustes-end-2026-10-13/pruebas/resultado.txt` y el registro final de Unity en `pruebas-unity-4.log`, en la misma carpeta de ajustes.
