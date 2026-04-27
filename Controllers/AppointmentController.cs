using Microsoft.AspNetCore.Mvc;
using apbd_s34366_cw7.DTOs;
using apbd_s34366_cw7.Services;

namespace apbd_s34366_cw7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentController(IAppointmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAppointments() 
        => Ok(await _service.GetAppointmentsAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var res = await _service.GetAppointmentByIdAsync(id);
        return res == null ? NotFound(new { Message = "Not found." }) : Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentRequestDto request)
    {
        if (request.AppointmentDate < DateTime.Now) return BadRequest("Date must be in the future.");
        if (string.IsNullOrEmpty(request.Reason) || request.Reason.Length > 250) return BadRequest("Invalid reason.");

        var result = await _service.CreateAppointmentAsync(request);
        return result switch
        {
            "UserNotFound" => BadRequest("Patient/Doctor not found or inactive."),
            "Conflict" => Conflict("Doctor is busy at this time."),
            "Created" => StatusCode(201),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var result = await _service.DeleteAppointmentAsync(id);
        return result switch
        {
            "NotFound" => NotFound(new { Message = "Appointment not found." }),
            "IsCompleted" => BadRequest("Cannot delete completed appointments."),
            _ => NoContent()
        };
    }
}