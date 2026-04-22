namespace apbd_cw7_git_s33244.DTOs;

public class UpdateAppointmentRequestDto
{
    public int idPatient { get; set; }
    public int idDoctor { get; set; }
    public DateTime appointmentDate { get; set; }
    public string status { get; set; } = string.Empty;
    public string reason { get; set; } = string.Empty;
    public string internalNotes { get; set; } = string.Empty;
}