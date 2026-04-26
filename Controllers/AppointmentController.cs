using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using apbd_s34366_cw7.DTOs;

namespace apbd_s34366_cw7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    
    public AppointmentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        await using var conn = new SqlConnection(GetConnectionString());

        var query = "SELECT a.IdAppointment, a.AppointmentDate, a.Status, " +
                  "p.FirstName + ' ' + p.LastName AS PatientName, " +
                  "d.FirstName + ' ' + d.LastName AS DoctorName " +
                  "FROM Appointments a " +
                  "JOIN Patients p ON a.IdPatient = p.IdPatient "+
                    "JOIN Doctors d ON a.IdDoctor = d.IdDoctor";
        var cmd = new SqlCommand(query, conn);
        var list = new List<AppointmentListDto>(); 
    
        await conn.OpenAsync();
        await using var r = await cmd.ExecuteReaderAsync();
    
        while (await r.ReadAsync())
        {
            list.Add(new AppointmentListDto 
            { 
                IdAppointment = (int)r["IdAppointment"], 
                AppointmentDate = (DateTime)r["AppointmentDate"], 
                Status = r["Status"].ToString()!,
                PatientFullName = r["PatientName"].ToString()!,
                DoctorFullName = r["DoctorName"].ToString()!
            });
        }

        return Ok(list);
    }
    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        await using var conn = new SqlConnection(GetConnectionString());
    
        string query = @"
            SELECT a.IdAppointment, a.AppointmentDate, a.Status,
                p.FirstName + ' ' + p.LastName AS PatientName,
                d.FirstName + ' ' + d.LastName AS DoctorName
                FROM Appointments a
                JOIN Patients p ON a.IdPatient = p.IdPatient
                JOIN Doctors d ON a.IdDoctor = d.IdDoctor
                WHERE a.IdAppointment = @Id";

        var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
        {
            return NotFound(new { Message = "Appointment not found." });
        }
        
        var res = new AppointmentListDto
        {
            IdAppointment = (int)reader["IdAppointment"],
            AppointmentDate = (DateTime)reader["AppointmentDate"],
            Status = reader["Status"].ToString()!,
            PatientFullName = reader["PatientName"].ToString()!,
            DoctorFullName = reader["DoctorName"].ToString()!
        };

        return Ok(res);
    }

    [HttpPost] 
    public async Task<IActionResult> CreateAppointment(CreateAppointmentRequestDto request) 
    {
    if (request.AppointmentDate < DateTime.Now) 
        return BadRequest("Date must be greather than today.");
    
    if (string.IsNullOrEmpty(request.Reason) || request.Reason.Length > 250)
        return BadRequest("Reason is required.");

    await using var conn = new SqlConnection(GetConnectionString());
    await conn.OpenAsync();

    var checkIfExistsCmd = new SqlCommand(
        "SELECT (SELECT COUNT(*) FROM Patients WHERE IdPatient = @IdP AND IsActive = 1) + " +
        "(SELECT COUNT(*) FROM Doctors WHERE IdDoctor = @IdD AND IsActive = 1)", conn);
    
    checkIfExistsCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
    checkIfExistsCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
    
    var conflictCmd = new SqlCommand(
        "SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdD AND AppointmentDate = @Date AND Status = 'Scheduled'", conn);
    
    conflictCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
    conflictCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);

    if ((int)await conflictCmd.ExecuteScalarAsync() > 0)
        return Conflict("Doctor is busy at this specific time.");

    var insertCmd = new SqlCommand(
        "INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, CreatedAt) " +
        "VALUES (@IdP, @IdD, @Date, 'Scheduled', @Reason, GETDATE())", conn);

    insertCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
    insertCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
    insertCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
    insertCmd.Parameters.AddWithValue("@Reason", request.Reason);

    await insertCmd.ExecuteNonQueryAsync();

    return StatusCode(201);
}
    
    
    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
    
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("Default Connection not found!");
        }
    
        return connectionString;
    }
}