using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarnetQRPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "EventRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_DoctorId",
                table: "EventRecords",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRecords_Doctors_DoctorId",
                table: "EventRecords",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRecords_Doctors_DoctorId",
                table: "EventRecords");

            migrationBuilder.DropIndex(
                name: "IX_EventRecords_DoctorId",
                table: "EventRecords");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "EventRecords");
        }
    }
}
