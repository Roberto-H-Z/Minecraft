# Terreno ampliado y explosiones

## Terreno

- Se amplió de **96 × 96** a **192 × 192 bloques**. Cada lado mide el doble; la superficie total es cuatro veces mayor.
- Los bloques siguen midiendo 2 unidades de Unity: el mapa ahora mide **384 × 384 unidades**.
- El relieve, lagos y minerales del área original conservan sus coordenadas en el centro del mapa. Se añadieron árboles en las zonas exteriores.
- Los guardados del mapa anterior de 96 × 96 se adaptan al cargar: se desplazan las coordenadas de los bloques y contenedores hacia el centro del mapa ampliado. Rodolfo, los cofres y las construcciones mantienen su posición en el mundo.
- La escena que ya estaba abierta con el tamaño anterior también se adapta cuando Unity recarga los scripts.

## Explosión del creeper

- El creeper mantiene su mecha y efecto de partículas existente.
- La onda tiene un alcance de **12 unidades** para daño y empuje. La fuerza se reduce con la distancia y la cobertura; una pared protege a Rodolfo.
- Cerca de la explosión, el daño máximo sigue siendo **la mitad de los corazones**.
- Rodolfo recibe un impulso horizontal y vertical con inercia. Durante un instante se limita el desplazamiento voluntario para que avanzar hacia el creeper no anule el empuje. Después recupera el control. El recorrido depende de la distancia, los obstáculos y la caída.
- El alcance máximo de destrucción es **3 bloques**, equivalentes a 6 unidades con el tamaño actual. El cráter es irregular y puede ser menor según la resistencia del material.
- La presión se calcula por rayos: tierra y arena son más frágiles que madera, y piedra y minerales resisten más. La capa inferior del mundo permanece protegida.
- El agua detiene la destrucción de bloques de la onda que la atraviesa. No anula por sí sola el daño a Rodolfo.
- Se eliminan las plantas que estaban sobre el suelo destruido. Los troncos afectados también se pueden romper.
- Aproximadamente un tercio de los bloques destruidos suelta materiales recogibles. La selección de materiales no depende de lo que Rodolfo lleve en la mano.
- Si se destruye un cofre o un horno, **su contenido completo se suelta**. Los objetos sueltos salen despedidos por la explosión.
- Cada sector afectado de 16 × 16 bloques actualiza su malla y colisión una sola vez por explosión. La vegetación también se actualiza por sectores; el resto del mapa no se reconstruye. Los cráteres quedan incluidos en el guardado del mundo.

## Ajustes en el Inspector del enemigo

| Campo | Valor inicial | Unidad |
| --- | --- | --- |
| Radio Explosion | 12 | Unidades de Unity, daño y empuje |
| Radio Destruccion | 3 | Bloques, alcance máximo de la presión |
| Fuerza Empuje Explosion | 6 | Velocidad horizontal multiplicada por el tamaño de bloque |
| Elevacion Explosion | 3.5 | Velocidad vertical multiplicada por el tamaño de bloque |

El comportamiento se inspira en la [definición oficial del creeper](https://learn.microsoft.com/en-us/minecraft/creator/reference/source/vanillabehaviorpack_snippets/entities/creeper?view=minecraft-bedrock-stable), que configura una explosión de potencia 3. La presión, cobertura y valores de empuje de este proyecto son una aproximación propia, no una reproducción exacta de todas las reglas del juego original. Para los cuerpos sueltos se utiliza [Rigidbody.AddExplosionForce de Unity 2023.2](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Rigidbody.AddExplosionForce.html); el empuje de Rodolfo se integra en su controlador de movimiento.

No se ejecutaron pruebas de juego en Play durante este cambio. La compilación usa el compilador C# y las referencias de Unity del proyecto, sin iniciar una segunda instancia del editor que ya está abierto.
