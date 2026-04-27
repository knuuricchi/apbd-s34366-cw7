using Microsoft.Data.SqlClient;
using apbd_s34366_cw7.DTOs;

namespace apbd_s34366_cw7.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IConfiguration _configuration;
    public AppointmentService(IConfiguration configuration) => _configuration = configuration;

    private string GetConnectionString() => _configuration.GetConnectionString("DefaultConnection")!;

    public async Task<IEnumerable<AppointmentListDto>> GetAppointmentsAsync()
    {
        await using var conn = new SqlConnection(GetConnectionString());
        var cmd = new SqlCommand(@"
            SELECT a.IdAppointment, a.AppointmentDate, a.Status,
            p.FirstName + ' ' + p.LastName AS PatientName,
            d.FirstName + ' ' + d.LastName AS DoctorName
            FROM Appointments a
            JOIN Patients p ON a.IdPatient = p.IdPatient
            JOIN Doctors d ON a.IdDoctor = d.IdDoctor", conn);
        
        var list = new List<AppointmentListDto>();
        await conn.OpenAsync();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new AppointmentListDto {
                IdAppointment = (int)r["IdAppointment"],
                AppointmentDate = (DateTime)r["AppointmentDate"],
                Status = r["Status"].ToString()!,
                PatientFullName = r["PatientName"].ToString()!,
                DoctorFullName = r["DoctorName"].ToString()!
            });
        }
        return list;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int id)
    {
        await using var conn = new SqlConnection(GetConnectionString());
        var cmd = new SqlCommand(@"
            SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, a.InternalNotes, a.CreatedAt,
            p.FirstName + ' ' + p.LastName AS PatientName, p.Email AS PatientEmail,
            d.FirstName + ' ' + d.LastName AS DoctorName, d.LicenseNumber
            FROM Appointments a
            JOIN Patients p ON a.IdPatient = p.IdPatient
            JOIN Doctors d ON a.IdDoctor = d.IdDoctor
            WHERE a.IdAppointment = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        return new AppointmentDetailsDto {
            IdAppointment = (int)r["IdAppointment"],
            AppointmentDate = (DateTime)r["AppointmentDate"],
            Status = r["Status"].ToString()!,
            Reason = r["Reason"].ToString()!,
            PatientFullName = r["PatientName"].ToString()!,
            PatientEmail = r["PatientEmail"].ToString()!,
            DoctorFullName = r["DoctorName"].ToString()!,
            LicenseNumber = r["LicenseNumber"].ToString()!,
            InternalNotes = r["InternalNotes"] == DBNull.Value ? null : r["InternalNotes"].ToString(),
            CreatedAt = (DateTime)r["CreatedAt"]
        };
    }

    public async Task<string> CreateAppointmentAsync(CreateAppointmentRequestDto request)
    {
        await using var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var checkCmd = new SqlCommand(@"
            SELECT (SELECT COUNT(*) FROM Patients WHERE IdPatient = @IdP AND IsActive = 1) + 
                   (SELECT COUNT(*) FROM Doctors WHERE IdDoctor = @IdD AND IsActive = 1)", conn);
        checkCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
        checkCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        if ((int)await checkCmd.ExecuteScalarAsync() < 2) return "UserNotFound";

        var conflictCmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdD 
            AND AppointmentDate = @Date AND Status = 'Scheduled'", conn);
        conflictCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        conflictCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
        if ((int)await conflictCmd.ExecuteScalarAsync() > 0) return "Conflict";

        var insertCmd = new SqlCommand(@"
            INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, CreatedAt)
            VALUES (@IdP, @IdD, @Date, 'Scheduled', @Reason, GETDATE())", conn);
        insertCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
        insertCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        insertCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
        insertCmd.Parameters.AddWithValue("@Reason", request.Reason);
        
        await insertCmd.ExecuteNonQueryAsync();
        return "Created";
    }

    public async Task<string> DeleteAppointmentAsync(int id)
    {
        await using var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var checkCmd = new SqlCommand(@"SELECT Status FROM Appointments WHERE IdAppointment = @Id", conn);
        checkCmd.Parameters.AddWithValue("@Id", id);
        await using var r = await checkCmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return "NotFound";
        var status = r["Status"].ToString();
        await r.CloseAsync();

        if (status == "Completed") return "IsCompleted";

        var deleteCmd = new SqlCommand(@"DELETE FROM Appointments WHERE IdAppointment = @Id", conn);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        await deleteCmd.ExecuteNonQueryAsync();
        return "Success";
    }
}