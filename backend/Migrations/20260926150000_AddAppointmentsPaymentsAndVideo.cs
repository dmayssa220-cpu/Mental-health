using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalHealth.API.Migrations;

[Migration("20260926150000_AddAppointmentsPaymentsAndVideo")]
public partial class AddAppointmentsPaymentsAndVideo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `DoctorAvailabilities` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `DoctorId` int NOT NULL,
                `DayOfWeek` int NOT NULL,
                `StartTime` time(6) NOT NULL,
                `EndTime` time(6) NOT NULL,
                `SlotDurationMinutes` int NOT NULL,
                `IsActive` tinyint(1) NOT NULL,
                CONSTRAINT `PK_DoctorAvailabilities` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_DoctorAvailabilities_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
                UNIQUE KEY `IX_DoctorAvailabilities_DoctorId_DayOfWeek_StartTime` (`DoctorId`, `DayOfWeek`, `StartTime`)
            ) CHARACTER SET=utf8mb4;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `Payments` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PatientId` int NOT NULL,
                `DoctorId` int NOT NULL,
                `Amount` decimal(65,30) NOT NULL,
                `Currency` longtext CHARACTER SET utf8mb4 NOT NULL,
                `Status` int NOT NULL,
                `StripeSessionId` longtext CHARACTER SET utf8mb4 NULL,
                `StripePaymentIntentId` longtext CHARACTER SET utf8mb4 NULL,
                `CreatedAt` datetime(6) NOT NULL,
                `PaidAt` datetime(6) NULL,
                CONSTRAINT `PK_Payments` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_Payments_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_Payments_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `Appointments` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `PatientId` int NOT NULL,
                `DoctorId` int NOT NULL,
                `ScheduledAt` datetime(6) NOT NULL,
                `DurationMinutes` int NOT NULL,
                `Type` int NOT NULL,
                `Status` int NOT NULL,
                `Reason` varchar(500) CHARACTER SET utf8mb4 NULL,
                `DoctorNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `PaymentId` int NULL,
                `Reminder24hSent` tinyint(1) NOT NULL,
                `Reminder1hSent` tinyint(1) NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                `UpdatedAt` datetime(6) NULL,
                CONSTRAINT `PK_Appointments` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_Appointments_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_Appointments_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
                KEY `IX_Appointments_DoctorId_ScheduledAt` (`DoctorId`, `ScheduledAt`)
            ) CHARACTER SET=utf8mb4;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `VideoSessions` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `AppointmentId` int NOT NULL,
                `RoomName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                `StartedAt` datetime(6) NULL,
                `EndedAt` datetime(6) NULL,
                CONSTRAINT `PK_VideoSessions` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_VideoSessions_Appointments_AppointmentId` FOREIGN KEY (`AppointmentId`) REFERENCES `Appointments` (`Id`) ON DELETE CASCADE
            ) CHARACTER SET=utf8mb4;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS `VideoSessions`; DROP TABLE IF EXISTS `Appointments`; DROP TABLE IF EXISTS `Payments`; DROP TABLE IF EXISTS `DoctorAvailabilities`;");
    }
}