using apbd_s34366_cw7.DTOs;

namespace apbd_s34366_cw7.Services;

public interface IAppointmentService
{
    Task<string> CreateAppointmentAsync(CreateAppointmentRequestDto request);
    Task<string> DeleteAppointmentAsync(int id);
    Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int id);
    Task<IEnumerable<AppointmentListDto>> GetAppointmentsAsync();
}