# AAA Slime Rancher VR

Prototipo en Unity inspirado en Slime Rancher, con movimiento VR, manos visibles, aspiradora, inventario, slimes, pollos y un estanque para recoger agua.

## Abrir el proyecto

1. Instala Git y Git LFS. Ejecuta `git lfs install` antes de clonar.
2. Clona el repositorio:

   ```bash
   git clone https://github.com/Samirlimachi/AAA-SlimeRancher.git
   cd AAA-SlimeRancher
   git lfs pull
   ```

3. Añade la carpeta clonada en Unity Hub y ábrela con **Unity 6000.5.6f1**, la versión indicada en `ProjectSettings/ProjectVersion.txt`.
4. Instala Blender para que Unity pueda importar los modelos `.blend` de `Assets/04_Models/POLLOS`. Los originales se trabajaron con Blender 5.2.1.
5. Espera a que Unity descargue los paquetes e importe los recursos.
6. Abre **`Assets/00_Scenes/AREA1.unity`** y pulsa Play.

El proyecto utiliza OpenXR y XR Interaction Toolkit. Incluye el simulador de interacción XR para las pruebas en el Editor. Para jugar con un visor, configura el runtime OpenXR del dispositivo.

## Controles VR de AREA1

| Acción | Control |
| --- | --- |
| Caminar | Joystick izquierdo |
| Correr | Pulsar el joystick izquierdo mientras te mueves |
| Saltar | A del mando derecho |
| Girar | Joystick derecho |
| Agarrar o soltar objetos y aspiradora | Grip |
| Aspirar objetos o cargar agua del estanque | Gatillo de la mano que sostiene la aspiradora |
| Lanzar el objeto seleccionado o disparar agua | Gatillo de la otra mano, sin aspirar a la vez |
| Cambiar ranura del inventario / volver desde agua | B del mando derecho |
| Seleccionar o deseleccionar el depósito de agua | X del mando izquierdo |

## Mecánicas actuales

- Caminar, correr con consumo de estamina y saltar.
- Manos VR y agarre de la aspiradora con cualquiera de las manos.
- Cuatro ranuras de inventario; vida, estamina, monedas, día y hora en el HUD.
- Aspirar y lanzar slimes, zanahorias, plorts, pollos y pollos viejos.
- Pollos que caminan y pueden alimentar a los slimes rosados para producir plorts.
- Estanque con agua recargable y depósito independiente de 30 unidades.
- Disparos de agua con salpicadura y empuje de objetos.

AREA1 tiene desactivada la carga y el guardado automáticos para las pruebas. En el Editor están disponibles F5 para guardar y F9 para cargar mediante el sistema de guardado existente.

## Carpetas principales

- `Assets/00_Scenes`: escenas, incluida AREA1.
- `Assets/01_Scripts/SlimeGameplay`: lógica del juego.
- `Assets/04_Models`: modelos y recursos del personaje y los objetos.
- `Assets/Area1`: componentes, prefabs y materiales específicos de AREA1.
- `Packages`: dependencias de Unity.
- `ProjectSettings`: configuración del proyecto.

Los modelos, imágenes y otros recursos binarios se almacenan con **Git LFS**. Las carpetas `Library`, `Logs`, `Temp` y `UserSettings` se generan localmente y no se incluyen en el repositorio.
