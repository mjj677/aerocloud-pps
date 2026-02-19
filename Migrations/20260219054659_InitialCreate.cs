using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AeroCloud.PPS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDeparture = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gate = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Passengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingReference = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckInStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passengers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BagDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagTagNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PassengerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagDrops_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Flights",
                columns: new[] { "Id", "Destination", "FlightNumber", "Gate", "Origin", "ScheduledDeparture", "Status" },
                values: new object[,]
                {
                    { 1, "AMS", "EZY1234", "B14", "MAN", new DateTime(2026, 2, 19, 15, 0, 0, 0, DateTimeKind.Utc), "Boarding" },
                    { 2, "LHR", "BA0456", "A07", "MAN", new DateTime(2026, 2, 19, 18, 0, 0, 0, DateTimeKind.Utc), "Scheduled" }
                });

            migrationBuilder.InsertData(
                table: "Passengers",
                columns: new[] { "Id", "BookingReference", "CheckInStatus", "CreatedAt", "FlightNumber", "FullName", "SeatNumber", "UpdatedAt" },
                values: new object[] { 1, "ABC123", "CheckedIn", new DateTime(2026, 2, 19, 12, 0, 0, 0, DateTimeKind.Utc), "EZY1234", "Jane Smith", "14A", new DateTime(2026, 2, 19, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_BagDrops_BagTagNumber",
                table: "BagDrops",
                column: "BagTagNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BagDrops_PassengerId",
                table: "BagDrops",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_FlightNumber",
                table: "Flights",
                column: "FlightNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_BookingReference",
                table: "Passengers",
                column: "BookingReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BagDrops");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "Passengers");
        }
    }
}
