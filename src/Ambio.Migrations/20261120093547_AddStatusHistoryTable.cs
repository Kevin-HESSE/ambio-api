using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambio.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_from = table.Column<string>(type: "text", nullable: false),
                    status_to = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_status_history_job_applications_job_application_id",
                        column: x => x.job_application_id,
                        principalTable: "job_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_status_history_job_application_id",
                table: "status_history",
                column: "job_application_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "status_history");
        }
    }
}
