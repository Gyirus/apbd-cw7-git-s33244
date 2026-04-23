namespace apbd_cw7_git_s33244.DTOs;

public class AppointmentDetailsDto
{
    
    public int idAppoitment { get; set; }
    public DateTime appoitmentDate { get; set; }
    public string status { get; set; } = string.Empty;
    public string reason { get; set; } = string.Empty;
    public string iternalNotes { get; set; } = string.Empty;
    public DateTime createdAt { get; set; }
    
    
    public string patientFullName { get; set; } = string.Empty;
    public string patientLastName { get; set; } = string.Empty;
    public string patientEmail { get; set; } = string.Empty;
    public string patientPhoneNumber { get; set; } = string.Empty;
    
    
    public string doctorFullName { get; set; } = string.Empty;
    
    public string doctorLastName { get; set; } = string.Empty;
    public string doctorLicenseNumber { get; set; } = string.Empty;
    public string specializationName { get; set; } = string.Empty;
}