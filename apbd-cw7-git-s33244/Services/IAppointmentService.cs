using apbd_cw7_git_s33244.DTOs;

namespace apbd_cw7_git_s33244.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName);
    Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int appointmentId);
    
    Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto request);
    
    Task<bool> UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto request);
    
    Task<bool> DeleteAppointmentAsync(int idAppointment);
}
