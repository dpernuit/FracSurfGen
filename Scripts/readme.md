- FractalSurfaceGenerator
    Handles the generation of the fractal and the meshes associated to it.
    As unity cannot gandle meshes bigger than 255x255, the fractal surface might be split in multiple gameObjects.
    Pressing "Space" will generate a new fractal.

- OrbitPanZoomScale
    A simple camera script that will, as the name suggests, orbit, pan, zoom and scale an object...
    LeftClick rotates around the object.
    RightClick pans the camera
    MouseWheel zooms in/out
    Shift+Click will scale the object along its Z-axis

- VoronoiGenerator
    Simple Voronoi diagram generator, using a maximum of 3 parameters.
    Press "Space" to generate a new diagram.
    Press "R" to rebuild the same diagram with new parameters (use the inspector).
    Attach the script to a quad to display the diagram.
