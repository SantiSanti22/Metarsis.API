using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Metarsis.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Edición de Encuestas (Admin)")]
    public class PreguntasController : ControllerBase
    {
        private readonly MetarsisDbContext _context;

        public PreguntasController(MetarsisDbContext context)
        {
            _context = context;
        }

        // 1. EDICIÓN DEL TEXTO DE UNA PREGUNTA
        [HttpPut("{preguntaId}")]
        public async Task<IActionResult> EditarPregunta(int preguntaId, [FromBody] PreguntaUpdateDto dto)
        {
            var pregunta = await _context.Set<PreguntaEncuesta>().FindAsync(preguntaId);
            if (pregunta == null) return NotFound("La pregunta no existe en la base de datos.");

            if (string.IsNullOrWhiteSpace(dto.NuevoTexto))
                return BadRequest("El texto de la pregunta no puede estar vacío.");

            pregunta.TextoPregunta = dto.NuevoTexto.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Pregunta actualizada con éxito." });
        }

        // 2. ELIMINACIÓN DE UNA PREGUNTA
        [HttpDelete("{preguntaId}")]
        public async Task<IActionResult> EliminarPregunta(int preguntaId)
        {
            var pregunta = await _context.Set<PreguntaEncuesta>().FindAsync(preguntaId);
            if (pregunta == null) return NotFound("La pregunta no existe en la base de datos.");

            _context.Set<PreguntaEncuesta>().Remove(pregunta);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Pregunta eliminada." });
        }

        // 3. REGISTRO MANUAL DE UNA NUEVA PREGUNTA
        [HttpPost]
        public async Task<IActionResult> AgregarPregunta([FromBody] PreguntaCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TextoPregunta))
                return BadRequest("El texto de la pregunta es obligatorio.");

            var nuevaPregunta = new PreguntaEncuesta
            {
                EncuestaID = dto.EncuestaID,
                TextoPregunta = dto.TextoPregunta.Trim()
            };

            _context.Set<PreguntaEncuesta>().Add(nuevaPregunta);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Pregunta agregada con éxito." });
        }
    }

    // DTOs Y MODELOS DE TRANSFERENCIA DE DATOS
    public class PreguntaUpdateDto
    {
        public string NuevoTexto { get; set; } = string.Empty;
    }

    public class PreguntaCreateDto
    {
        public int EncuestaID { get; set; }
        public string TextoPregunta { get; set; } = string.Empty;
    }
}