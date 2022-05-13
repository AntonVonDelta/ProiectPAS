# ProiectPAS
 - Jocul construit de echipa noastra simuleaza o lume subacvatica asa cum e vazuta dintr-un submarin.
 - Editorul folosit este Unity cu resurse, pachete si texturi descarcate de pe store.
 - Controlul se face din tastele WASD si din mouse pentru camera.

 # Features
  - Algoritm de Marching Cubes
    - pentru generarea randomizata a terenului
    - implementat complet intr-un compute shader
    - foloseste perlin noise
  - Verlet Integration
    - pentru simularea plantelor subacvatice
    - include algoritm de coliziune cu obiectele din jur (precum submarinul)
    - constrangeri pentru a crea un aspect cat mai fidel al unei plante
    - simulare fortei lui Arhimede
  - Chunk Generator
    - genereaza chunkuri dupa datele returnate de Marching Cubes
    - caching
    - algoritm de amplasare randomizata a plantelor   
  - Caustics
    - pentru simularea valurilor pe fundul oceanului 
  - Sistem de particule


# Imagini

![image](https://user-images.githubusercontent.com/25268629/168335669-b891dfb3-75a2-4894-af01-5939ea8b960f.png)

![image](https://user-images.githubusercontent.com/25268629/168335432-8a594758-c055-4536-b90d-d0ec73a72dfd.png)

