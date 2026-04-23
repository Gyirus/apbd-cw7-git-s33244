namespace apbd_cw7_git_s33244.DTOs;

public class AppointmentListDto
{
    public int idAppoitment { get; set; }
    public DateTime appoitmentDate { get; set; }
    public string status { get; set; } = string.Empty;
    public string reason { get; set; } = string.Empty;
    public string patientFullName { get; set; } = string.Empty;
    public string patientLastName { get; set; } = string.Empty;
    public string patientEmail { get; set; } = string.Empty;
}