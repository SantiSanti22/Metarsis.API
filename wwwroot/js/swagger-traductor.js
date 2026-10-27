window.addEventListener('load', function () {
    setTimeout(function () {
        const traducciones = {
            "Parameters": "Parámetros",
            "No parameters": "Sin parámetros",
            "Try it out": "Probar",
            "Cancel": "Cancelar",
            "Execute": "Ejecutar",
            "Responses": "Respuestas",
            "Response body": "Cuerpo de la respuesta",
            "Response headers": "Encabezados de respuesta",
            "Code": "Código",
            "Description": "Descripción",
            "Links": "Enlaces",
            "Value": "Valor",
            "Data type": "Tipo de dato",
            "Parameter type": "Tipo de parámetro"
        };

        // Función recursiva para buscar y reemplazar textos
        function traducirElementos(nodo) {
            if (nodo.nodeType === Node.TEXT_NODE) {
                let textoOriginal = nodo.nodeValue.trim();
                if (traducciones[textoOriginal]) {
                    nodo.nodeValue = nodo.nodeValue.replace(textoOriginal, traducciones[textoOriginal]);
                }
            } else {
                for (let hijo of nodo.childNodes) {
                    traducirElementos(hijo);
                }
            }
        }

        traducirElementos(document.body);

        // Observer para cambios dinámicos (cuando despliegas un endpoint)
        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) {
                        traducirElementos(node);
                    }
                });
            });
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }, 500);
});