# Mejoras del juego

## Personaje y movimiento

- Rodolfo de pie mide exactamente **2 bloques**: con bloques de 2 unidades, su modelo visible y su cápsula de colisión miden 4 unidades. La medida se calcula a partir del terreno.
- Se normalizó la escala para evitar deformaciones. Zombies y esqueletos usan esa altura; el creeper conserva su tamaño menor.
- Movimiento relativo a la cámara en las tres perspectivas, aceleración y frenado, carreras con velocidad estable y salto de aproximadamente 1,18 bloques.
- El salto conserva brevemente una pulsación anticipada y permite saltar justo al abandonar un borde.
- Ctrl agacha a Rodolfo, reduce su altura a 1,5 bloques y su velocidad. Al agacharse no camina fuera de un borde y no se levanta bajo un techo bajo.
- Salto automático con comprobaciones del obstáculo y del espacio disponible para la cabeza.
- Natación con Espacio, descenso con Ctrl, aire limitado y daño por ahogamiento. Las caídas de más de tres bloques causan daño; caer al agua lo evita.

## Supervivencia, criaturas y objetos

- Caminar, correr y saltar gastan alimento gradualmente. La saturación retrasa el consumo de hambre y la regeneración funciona cuando hay suficiente comida.
- La carne podrida recupera hambre, pero aumenta temporalmente su consumo.
- Los monstruos detectan con visión y distancia, mantienen la persecución y buscan rodear obstáculos. Su velocidad no aumenta cuando Rodolfo corre.
- Los zombies atacan por alcance, con intervalo entre golpes. Los animales huyen al recibir daño. Se mantienen los cuatro golpes para derrotarlos.
- El creeper parpadea y se hincha antes de explotar; conserva el daño de media vida.
- Animales y monstruos sueltan su botín en el suelo. Los objetos caen con gravedad, giran, se acercan al jugador al recogerlos y no atraviesan paredes para ser recogidos. Caducan después de cinco minutos.
- Botín recogido da experiencia, visible en una barra y un contador de niveles.
- El tridente usa gravedad, orienta su punta durante el vuelo, se clava al impactar y vuelve a Rodolfo. Las tres herramientas siguen siendo permanentes.
- Minado con duración diferente para tierra, arena, madera, piedra y minerales; el pico acelera el trabajo. Hay grietas, balanceo de la mano, fragmentos y efectos de sonido.
- El contorno blanco se ajusta al bloque o al tronco apuntado.

## Interfaz y ambiente

- Primera persona al comenzar; F5/V alterna primera persona, cámara trasera y cámara frontal. Las cámaras exteriores se acercan cuando hay una pared detrás.
- Balanceo suave al caminar y aumento del campo de visión al correr, con cuerpo oculto en primera persona.
- Barra de objetos, nombres temporales al seleccionar, recarga de ataque, hambre de derecha a izquierda, aire, experiencia y ayuda opcional.
- Materiales en pilas de hasta 64, seis espacios dinámicos en la barra y 27 espacios de reserva. Si el inventario está lleno, los objetos quedan en el suelo. Pulsar un material del inventario lo asigna a la barra.
- Pausa con ajustes de sensibilidad, balanceo, volumen y salto automático.
- Sol y luna con texturas del paquete proporcionado, estrellas nocturnas, nubes en movimiento y niebla de distancia.
- Sonidos sintéticos para pasos, golpes, minado, construcción, comida, recogida y explosiones.

## Controles

| Acción | Control |
|---|---|
| Mover / correr / agacharse | WASD / Shift / Ctrl |
| Saltar o subir en el agua | Espacio |
| Atacar | Un clic izquierdo |
| Picar | Mantener clic izquierdo |
| Colocar un bloque | Un clic derecho |
| Comer con hambre y comida en la mano | Mantener clic derecho |
| Lanzar tridente | Mantener clic derecho para cargar y soltarlo |
| Seleccionar objeto | 1–9 o rueda |
| Soltar una unidad de un objeto recogido | Q |
| Inventario | E o I |
| Cambiar perspectiva | F5 o V |
| Ayuda / coordenadas | F1 / F3 |
| Pausa / cerrar inventario | Esc |

## Guardado

En **Esc → Guardar mundo** se conservan las modificaciones del terreno, troncos talados, plantas quitadas, posición de Rodolfo, inventario, vida, hambre y experiencia. **Cargar guardado** restaura ese archivo. Al pulsar Play se sigue creando una partida nueva con las tres herramientas; cargar una anterior es opcional.

El archivo está en `Application.persistentDataPath/mundo-rodolfo.json`. El guardado exige la misma semilla, dimensiones y tamaño de bloque. La fauna y los esqueletos generados se recrean al cargar; no se conserva el estado de las criaturas ni los objetos sueltos.

## Comprobación

Se compiló el proyecto con Unity 2023.2.20f1. La compilación final terminó sin errores. No se realizaron pruebas de jugabilidad en Play durante este ajuste.

Respaldo anterior a los cambios: `.codex-backups/mejoras-2026-10-03/`.
