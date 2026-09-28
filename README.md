# AAA Slime Rancher VR

Experiencia de Realidad Virtual para **Oculus Quest** hecha en **Unity** con XR Interaction Toolkit, inspirada en *Slime Rancher*: el jugador cuida su rancho, alimenta slimes, vende plorts, mejora su equipo y defiende el rancho de oleadas de slimes malos.

Proyecto de auto aprendizaje de la materia **Realidad Virtual y Aumentada**.

## Equipo

- Samir Limachi López
- Tanina Magdiel Solís Quispe

## Abrir el proyecto

1. Instala Git y Git LFS. Ejecuta `git lfs install` antes de clonar.
2. Clona el repositorio:

   ```bash
   git clone https://github.com/Samirlimachi/AAA-SlimeRancher.git
   cd AAA-SlimeRancher
   git lfs pull
   ```

3. Añade la carpeta en Unity Hub y ábrela con **Unity 6000.5.6f1** (`ProjectSettings/ProjectVersion.txt`).
4. Espera a que Unity descargue los paquetes e importe los recursos.
5. Pulsa **Play**: el juego empieza siempre en **`Assets/00_Scenes/MAIN_MENU.unity`**, igual que en el build. Se puede desactivar en *Beatrix → Iniciar Play desde MAIN_MENU*.

Sin visor se prueba con el **XR Interaction Simulator** (teclado y mouse). Con visor, configura el runtime OpenXR del Quest (Link). Para el build de Android instala Android Build Support, SDK, NDK y OpenJDK desde Unity Hub.

Los modelos, imágenes y sonidos se guardan con **Git LFS**. `Library`, `Logs`, `Temp` y `UserSettings` se generan localmente y no se suben (ver `.gitignore`).

## Estructura de carpetas

```
Assets/
├── 00_Scenes/        MAIN_MENU (inicio) y AREA1 (juego)
├── 01_Scripts/
│   ├── Area1/        gameplay de AREA1: tiendas, oleadas, jefe, HUD, sonidos, efectos (+ Editor/)
│   ├── SlimeGameplay/ núcleo: RanchGame (guardado), items, slimes, aspiradora (+ Editor/)
│   └── SlimeMenus/   menú de inicio/pausa y opciones (+ Editor/)
├── 02_Prefabs/       Items, Enemigos, Herramientas, Entorno
├── 03_SO/            Scriptable Objects (Items, Slimes, Menus, Resources/SonidosJuego)
├── 04_Models/        modelos OBJ/FBX, recolectores y mallas generadas
├── 05_Sonidos/       efectos y música
├── 06_Materiales/    materiales, texturas y shaders (Shaders/Resources para Shader.Find)
└── 07_UI/            logo y material del HUD
```

`Samples`, `TextMesh Pro`, `VRTemplateAssets`, `XR`, `XRI`, `Settings` y `CompositionLayers` pertenecen a Unity y a los paquetes XR: no se mueven.

## Scriptable Objects

| Asset | Tipo | Para qué |
| --- | --- | --- |
| `03_SO/Items/*.asset` (zanahoria, pollo, pollo viejo, corazón, plort, slimes) | `RanchItemData` | Nombre, color, prefab, límite por ranura, valor de venta y plorts que da al comerla. Una sola lógica sirve para todos los objetos. |
| `03_SO/Slimes/SlimeRosadoData.asset` | `SlimeData` | Salto del slime y cómo lo atrapa la aspiradora (alcance, ángulo y velocidad de succión). |
| `03_SO/Resources/SonidosJuego.asset` | `Area1SoundBank` | Qué sonido y volumen usa cada evento (música, compras, slimes, jugador...). |
| `03_SO/Menus/SlimeMenuTheme.asset` | `SlimeMenuTheme` | Colores, fuente y logo del menú. |

## Sistema de guardado

- **Guardar partida** (menú de pausa) y **Volver al menú** guardan en un JSON (`AREA1_rancho_v1.json` en `Application.persistentDataPath`): monedas, vida, inventario, posición y objetos del mundo.
- **Continuar partida** (menú principal) carga ese archivo al entrar a AREA1.
- Mejoras compradas, oleadas completadas y opciones (volumen, música, vibración, giro) se guardan en `PlayerPrefs`.
- **Nueva partida** pide confirmación y borra la partida y el progreso.

Flujo de prueba: jugar → Guardar → cerrar el juego → Continuar partida → el progreso vuelve.

## Controles VR

| Acción | Control |
| --- | --- |
| Agarrar / soltar la aspiradora | Grip (una vez agarra, otra vez suelta) |
| Agarrar objetos | Mantener Grip |
| Aspirar objetos o cargar agua del estanque | Gatillo de la mano con la aspiradora |
| Lanzar objeto | Gatillo de la otra mano |
| Disparar agua | Ranura 5 (agua) + gatillo |
| Cambiar ranura | A siguiente / B anterior |
| Moverse / correr | Joystick izquierdo / hundirlo para correr |
| Girar / teletransporte | Joystick derecho a los lados / adelante |
| Tiendas y tablero de oleadas | Apuntar al botón y Grip |
| Menú | Botón de tres rayas del mando izquierdo |

En PC (simulador): WASD para moverse, Shift izquierdo para correr, clic derecho para mirar, Esc para el menú.

## Mecánicas

- **Rancho:** aspirar y lanzar objetos, alimentar slimes rosados (zanahoria 1 plort, pollo 2, pollo viejo 3) y vender plorts en el recolector.
- **Tiendas:** comida y corazones de vida; mejoras permanentes de vida, daño de agua, tanque de agua y recolector.
- **Oleadas:** 5 niveles de slimes malos que se desbloquean y compran; el nivel 5 trae al jefe, que escupe baba e invoca slimes. Se neutralizan con agua.
- **Feedback:** HUD con vida, estamina y monedas, carteles de victoria/derrota, partículas, sonidos y música.
