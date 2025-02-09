using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RepoLayer.Entity;
using BusinessLayer.Interfaces;
using ModelLayer;

namespace FundooNotes.Controllers
{
    [Route("/label")]
    [ApiController]
    public class LabelController : ControllerBase
    {
        private readonly ILabelBL _labelBL;

        public LabelController(ILabelBL labelBL)
        {
            _labelBL = labelBL;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLabels()
        {
            var labels = await _labelBL.GetAllLabelsAsync();
            return Ok(labels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLabelById(int id)
        {
            var label = await _labelBL.GetLabelByIdAsync(id);
            if (label == null)
                return NotFound("Label not found.");
            return Ok(label);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLabel([FromBody] LabelInputModel labelInput)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdLabel = await _labelBL.CreateLabelAsync(labelInput);
            return CreatedAtAction(nameof(GetLabelById), new { id = createdLabel.LabelId }, createdLabel);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignLabelToNote([FromQuery] int noteId, [FromQuery] int labelId)
        {
            var result = await _labelBL.AssignLabelToNoteAsync(noteId, labelId);
            if (!result)
                return BadRequest("Invalid note or label ID.");
            return Ok("Label assigned successfully.");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveLabelFromNote([FromQuery] int noteId, [FromQuery] int labelId)
        {
            var result = await _labelBL.RemoveLabelFromNoteAsync(noteId, labelId);
            if (!result)
                return NotFound("Label or note not found.");
            return Ok("Label removed successfully.");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLabel(int id)
        {
            var result = await _labelBL.DeleteLabelAsync(id);
            if (!result)
                return NotFound("Label not found or already deleted.");

            return Ok("Label deleted successfully.");
        }

    }
}
