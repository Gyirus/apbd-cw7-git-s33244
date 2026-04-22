namespace apbd_cw7_git_s33244.DTOs;

public class CreateAppointmentRequestDto
{
    public int idPatient { get; set; }
    public int idDoctor { get; set; }
    public DateTime фppointmentDate { get; set; }
    public string кeason { get; set; } = string.Empty;
}
