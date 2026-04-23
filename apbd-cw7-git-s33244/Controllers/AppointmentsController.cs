using apbd_cw7_git_s33244.DTOs;
using apbd_cw7_git_s33244.Services;
using Microsoft.AspNetCore.Mvc;

namespace apbd_cw7_git_s33244.Controllers;


[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentListDto>>> GetAppointments([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync(status, patientLastName);
        return Ok(appointments);
    }

    [HttpGet("{idAppointment:int}")]
    public async Task<ActionResult<AppointmentDetailsDto>> GetAppointment(int idAppointment)
    {
        var appointment = await _appointmentService.GetAppointmentDetailsAsync(idAppointment);
        if (appointment == null)
            return NotFound(new ErrorResponseDto { message = $"Appointment with id {idAppointment} not found." });
        return Ok(appointment);
    }


    [HttpPost]
    public async Task<ActionResult<object>> CreateAppointment([FromBody] CreateAppointmentRequestDto request)
    {
        try
        {
            int newId = await _appointmentService.CreateAppointmentAsync(request);
            return CreatedAtAction(nameof(GetAppointment), new { idAppointment = newId }, new { idAppointment = newId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponseDto { message = ex.Message });
        }
    }


    [HttpPut("{idAppointment:int}")]
    public async Task<ActionResult> UpdateAppointment(int idAppointment, [FromBody] UpdateAppointmentRequestDto request)
    {
        try
        {
            bool updated = await _appointmentService.UpdateAppointmentAsync(idAppointment, request);
            if (!updated)
                return NotFound(new ErrorResponseDto { message = $"Appointment {idAppointment} not found." });
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot change date"))
        {
            return BadRequest(new ErrorResponseDto { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponseDto { message = ex.Message });
        }
    }


    [HttpDelete("{idAppointment:int}")]
    public async Task<ActionResult> DeleteAppointment(int idAppointment)
    {
        try
        {
            bool deleted = await _appointmentService.DeleteAppointmentAsync(idAppointment);
            if (!deleted)
                return NotFound(new ErrorResponseDto { message = $"Appointment {idAppointment} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex) 
        {
            return Conflict(new ErrorResponseDto { message = ex.Message });
        }
    }
}