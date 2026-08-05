using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationalReportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentimentScore = table.Column<int>(type: "int", nullable: false),
                    BrandPositioning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditReports_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditCompetitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditCompetitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditCompetitors_AuditReports_AuditReportId",
                        column: x => x.AuditReportId,
                        principalTable: "AuditReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditRecommendedKeywords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecommendedKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditRecommendedKeywords_AuditReports_AuditReportId",
                        column: x => x.AuditReportId,
                        principalTable: "AuditReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditCompetitors_AuditReportId",
                table: "AuditCompetitors",
                column: "AuditReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecommendedKeywords_AuditReportId",
                table: "AuditRecommendedKeywords",
                column: "AuditReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditReports_CampaignId",
                table: "AuditReports",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditCompetitors");

            migrationBuilder.DropTable(
                name: "AuditRecommendedKeywords");

            migrationBuilder.DropTable(
                name: "AuditReports");
        }
    }
}
