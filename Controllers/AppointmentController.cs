using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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
    
        var sql = "SELECT a.IdAppointment, a.AppointmentDate, a.Status, " +
                  "p.FirstName + ' ' + p.LastName AS PatientName, " +
                  "d.FirstName + ' ' + d.LastName AS DoctorName " +
                  "FROM Appointments a " +
                  "JOIN Patients p ON a.IdPatient = p.IdPatient " +
                  "JOIN Doctors d ON a.IdDoctor = d.IdDoctor";

        var cmd = new SqlCommand(sql, conn);
    
        var list = new List<object>();
    
        await conn.OpenAsync();
    
        await using var r = await cmd.ExecuteReaderAsync();
    
        while (await r.ReadAsync())
        {
            list.Add(new { 
                IdAppointment = r["IdAppointment"], 
                Date = r["AppointmentDate"], 
                Status = r["Status"],
                Patient = r["PatientName"],
                Doctor = r["DoctorName"]
            });
        }

        return Ok(list);
    }
    
    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
               ?? "Server=localhost,1433;Database=ClinicAdoNet;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";
    }
}