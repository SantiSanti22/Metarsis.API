import os
from PIL import Image

# Obtener la ruta exacta de la carpeta donde reside este script
directorio_actual = os.path.dirname(os.path.abspath(__file__))
img_entrada = os.path.join(directorio_actual, "Avenzo_Logo_Header.png")
img_salida = os.path.join(directorio_actual, "Avenzo_Logo_Dark.png")

# Cargar la imagen
img = Image.open(img_entrada).convert("RGBA")
pixels = img.load()
width, height = img.size

for y in range(height):
    for x in range(width):
        r, g, b, a = pixels[x, y]
        
        # Ignorar píxeles transparentes
        if a == 0:
            continue
            
        # Detectar el texto oscuro ("avenzo")
        es_pixel_oscuro = r < 75 and g < 75 and b < 75
        
        if es_pixel_oscuro:
            # Transformar a blanco puro manteniendo el canal alfa (suavizado)
            pixels[x, y] = (255, 255, 255, a)

# Guardar la versión para modo oscuro en la misma carpeta
img.save(img_salida, format="PNG")
print("✅ 'Avenzo_Logo_Dark.png' generado exitosamente.")