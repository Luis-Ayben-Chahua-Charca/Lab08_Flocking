# Laboratorio 08 Flocking

Proyecto de Desarrollo de Software para Juegos de la Universidad Nacional de San Agustín de Arequipa. Incluye un cardumen 3D y una escena de persecución que aplica cohesión, separación y alineamiento.

## Ejecutar en Unity

1. Abrir la carpeta del repositorio desde Unity Hub con **Unity 6000.6.2f1**, la versión indicada en `ProjectSettings/ProjectVersion.txt`.
2. Esperar a que Unity importe los recursos y abra el proyecto en URP.
3. Abrir una de las siguientes escenas desde la ventana Project y pulsar Play.

| Escena | Prueba | Controles y parámetros |
|---|---|---|
| [`Lab08_Flocking`](Assets/Scenes/Lab08_Flocking.unity) | Cardumen de peces y evasión de obstáculos. | `WASD` para mover la cámara y ratón para orientarla. Los pesos y radios se ajustan en el objeto `Flock`. |
| [`SampleScene` de FlockingBoids](Assets/FlockingBoids/Assets/Scenes/SampleScene.unity) | Persecución grupal, perímetro de aceptación y pausa antes de reanudar la persecución. | Clic izquierdo sobre el suelo para mover al jugador. Shift izquierdo para detenerlo y orientarlo hacia el cursor. Los ajustes del grupo están en `Enemy Group`. |

## Material de referencia

- [GameDevChef/Flocking](https://github.com/GameDevChef/Flocking), proyecto base del cardumen.
- [Tutorial adicional de persecución con boids](https://www.youtube.com/watch?v=Srg6GRFspps), escena base y ejercicio de seguimiento grupal.
