using System.Data;
using apbd_cw7_git_s33244.DTOs;
using Microsoft.Data.SqlClient;

namespace apbd_cw7_git_s33244.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
    }
    
    
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName)
    {
        var appointments = new List<AppointmentListDto>();
    
        const string sql = @"
    SELECT
        a.IdAppointment,
        a.AppointmentDate,
        a.Status,
        a.Reason,
        p.FirstName + N' ' + p.LastName AS PatientFullName,
        p.LastName AS PatientLastName,  -- Добавлено поле LastName
        p.Email AS PatientEmail
    FROM dbo.Appointments a
    INNER JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
    WHERE (@Status IS NULL OR a.Status = @Status)
      AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
    ORDER BY a.AppointmentDate;
";
    
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
    
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50)
            .Value = status ?? (object)DBNull.Value;
        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 100)
            .Value = patientLastName ?? (object)DBNull.Value;
    
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var appointment = new AppointmentListDto()
            {
                idAppoitment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                appoitmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                status = reader.GetString(reader.GetOrdinal("Status")),
                reason = reader.GetString(reader.GetOrdinal("Reason")),
                patientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
                patientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),  // Добавлено
                patientEmail = reader.GetString(reader.GetOrdinal("PatientEmail"))
            };
            appointments.Add(appointment);
        }
        return appointments;
    }

 public async Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id)
{
    const string sql = @"
    SELECT 
        a.IdAppointment,
        a.AppointmentDate,
        a.Status,
        a.Reason,
        a.InternalNotes,
        a.CreatedAt,
        p.FirstName AS PatientFirstName,
        p.LastName AS PatientLastName,
        p.FirstName + ' ' + p.LastName AS PatientFullName,
        p.Email AS PatientEmail,
        p.PhoneNumber AS PatientPhone,
        d.FirstName AS DoctorFirstName,
        d.LastName AS DoctorLastName,
        d.FirstName + ' ' + d.LastName AS DoctorFullName,
        d.LicenseNumber AS DoctorLicenseNumber,
        s.Name AS SpecializationName
    FROM dbo.Appointments a
    INNER JOIN dbo.Patients p ON a.IdPatient = p.IdPatient
    INNER JOIN dbo.Doctors d ON a.IdDoctor = d.IdDoctor
    INNER JOIN dbo.Specializations s ON d.IdSpecialization = s.IdSpecialization
    WHERE a.IdAppointment = @IdAppointment;
";
    
    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand(sql, connection);
    
    command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = id;
    
    await connection.OpenAsync();
    await using var reader = await command.ExecuteReaderAsync();
    
    if (!await reader.ReadAsync()) return null;
    
    return new AppointmentDetailsDto
    {
        idAppoitment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
        appoitmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
        status = reader.GetString(reader.GetOrdinal("Status")),
        reason = reader.GetString(reader.GetOrdinal("Reason")),
        iternalNotes = reader.IsDBNull(reader.GetOrdinal("InternalNotes")) 
            ? string.Empty 
            : reader.GetString(reader.GetOrdinal("InternalNotes")),
        createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        patientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
        patientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
        patientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
        patientPhoneNumber = reader.IsDBNull(reader.GetOrdinal("PatientPhone")) 
            ? string.Empty 
            : reader.GetString(reader.GetOrdinal("PatientPhone")),
        doctorFullName = reader.GetString(reader.GetOrdinal("DoctorFullName")),
        doctorLastName = reader.GetString(reader.GetOrdinal("DoctorLastName")),
        doctorLicenseNumber = reader.IsDBNull(reader.GetOrdinal("DoctorLicenseNumber")) 
            ? string.Empty 
            : reader.GetString(reader.GetOrdinal("DoctorLicenseNumber")),
        specializationName = reader.IsDBNull(reader.GetOrdinal("SpecializationName")) 
            ? string.Empty 
            : reader.GetString(reader.GetOrdinal("SpecializationName"))
    };
}
    
    
      public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto request)
    {
        if (request.appointmentDate <= DateTime.Now)
            throw new ArgumentException("Appointment date cannot be in the past.");

        if (string.IsNullOrWhiteSpace(request.reason) || request.reason.Length > 250)
            throw new ArgumentException("Reason must be 1-250 characters.");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!await IsPatientActiveAsync(connection, request.idPatient))
            throw new ArgumentException("Patient does not exist or is inactive.");

        if (!await IsDoctorActiveAsync(connection, request.idDoctor))
            throw new ArgumentException("Doctor does not exist or is inactive.");

        if (await HasDoctorAppointmentAtTimeAsync(connection, request.idDoctor, request.appointmentDate, null))
            throw new ConflictException("Doctor already has an appointment at this time.");

        const string insertSql = @"
            INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes, CreatedAt)
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason, NULL, GETUTCDATE());
            SELECT SCOPE_IDENTITY();
        ";

        await using var command = new SqlCommand(insertSql, connection);
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.idPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.appointmentDate;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.reason;

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return newId;
    }

    public async Task<bool> UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentStatus = await GetAppointmentStatusAsync(connection, idAppointment);
        if (currentStatus == null) return false;

        if (currentStatus == "Completed" && request.appointmentDate != default)
            throw new InvalidOperationException("Cannot change date of a completed appointment.");

        if (!await IsPatientActiveAsync(connection, request.idPatient))
            throw new ArgumentException("Patient does not exist or is inactive.");
        if (!await IsDoctorActiveAsync(connection, request.idDoctor))
            throw new ArgumentException("Doctor does not exist or is inactive.");

        var allowedStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
        if (!allowedStatuses.Contains(request.status))
            throw new ArgumentException("Invalid status value. Allowed: Scheduled, Completed, Cancelled.");

        if (request.appointmentDate != default)
        {
            if (await HasDoctorAppointmentAtTimeAsync(connection, request.idDoctor, request.appointmentDate, idAppointment))
                throw new ConflictException("Doctor already has an appointment at this time.");
        }

        const string updateSql = @"
            UPDATE dbo.Appointments
            SET IdPatient = @IdPatient,
                IdDoctor = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status = @Status,
                Reason = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
        ";

        await using var command = new SqlCommand(updateSql, connection);
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.idPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.appointmentDate;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = request.status;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.reason;
        command.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, -1).Value = (object?)request.internalNotes ?? DBNull.Value;
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAppointmentAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var status = await GetAppointmentStatusAsync(connection, idAppointment);
        if (status == null) return false;
        if (status == "Completed")
            throw new InvalidOperationException("Cannot delete a completed appointment.");

        const string deleteSql = "DELETE FROM dbo.Appointments WHERE IdAppointment = @IdAppointment;";
        await using var command = new SqlCommand(deleteSql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }
    
    
    
    
    private async Task<bool> IsPatientActiveAsync(SqlConnection connection, int idPatient)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;
        var count = (int)await cmd.ExecuteScalarAsync();
        return count == 1;
    }

    private async Task<bool> IsDoctorActiveAsync(SqlConnection connection, int idDoctor)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        var count = (int)await cmd.ExecuteScalarAsync();
        return count == 1;
    }

    private async Task<bool> HasDoctorAppointmentAtTimeAsync(SqlConnection connection, int idDoctor, DateTime appointmentDate, int? excludeAppointmentId)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM dbo.Appointments
            WHERE IdDoctor = @IdDoctor
              AND AppointmentDate = @AppointmentDate
              AND Status NOT IN ('Cancelled')";
        
        if (excludeAppointmentId.HasValue)
            sql += " AND IdAppointment != @ExcludeId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        cmd.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;
        if (excludeAppointmentId.HasValue)
            cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = excludeAppointmentId.Value;

        var count = (int)await cmd.ExecuteScalarAsync();
        return count > 0;
    }

    private async Task<string?> GetAppointmentStatusAsync(SqlConnection connection, int idAppointment)
    {
        const string sql = "SELECT Status FROM dbo.Appointments WHERE IdAppointment = @IdAppointment";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;
        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}