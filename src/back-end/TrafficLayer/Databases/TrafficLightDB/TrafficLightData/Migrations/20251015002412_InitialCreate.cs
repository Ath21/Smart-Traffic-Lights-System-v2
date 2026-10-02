using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrafficLightData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "intersections",
                columns: table => new
                {
                    IntersectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    LightCount = table.Column<int>(type: "int", nullable: false),
                    MappedLightIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intersections", x => x.IntersectionId);
                });

            migrationBuilder.CreateTable(
                name: "traffic_configurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CycleDurationSec = table.Column<int>(type: "int", nullable: false),
                    GlobalOffsetSec = table.Column<int>(type: "int", nullable: false),
                    PhaseDurationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IntersectionEntityIntersectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traffic_configurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_traffic_configurations_intersections_IntersectionEntityIntersectionId",
                        column: x => x.IntersectionEntityIntersectionId,
                        principalTable: "intersections",
                        principalColumn: "IntersectionId");
                });

            migrationBuilder.CreateTable(
                name: "traffic_lights",
                columns: table => new
                {
                    LightId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntersectionId = table.Column<int>(type: "int", nullable: false),
                    LightName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    IsOperational = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traffic_lights", x => x.LightId);
                    table.ForeignKey(
                        name: "FK_traffic_lights_intersections_IntersectionId",
                        column: x => x.IntersectionId,
                        principalTable: "intersections",
                        principalColumn: "IntersectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "intersections",
                columns: new[] { "IntersectionId", "CreatedAt", "IsActive", "Latitude", "LightCount", "Location", "Longitude", "MappedLightIdsJson", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), true, 38.004677m, 2, "Agiou Spyridonos & Dimitsanas Street", 23.676086m, "[101, 102]", "Agiou Spyridonos" },
                    { 2, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), true, 38.003558m, 2, "Eastern Gate", 23.678042m, "[201, 202]", "Anatoliki Pyli" },
                    { 3, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), true, 38.002644m, 3, "Western Gate", 23.674499m, "[301, 302, 303]", "Dytiki Pyli" },
                    { 4, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), true, 38.001580m, 3, "Church Intersection", 23.673638m, "[401, 402, 403]", "Ekklisia" },
                    { 5, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), true, 38.004456m, 2, "Central Gate", 23.676483m, "[501, 502]", "Kentriki Pyli" }
                });

            migrationBuilder.InsertData(
                table: "traffic_configurations",
                columns: new[] { "ConfigurationId", "CycleDurationSec", "GlobalOffsetSec", "IntersectionEntityIntersectionId", "LastUpdated", "Mode", "PhaseDurationsJson", "Purpose" },
                values: new object[,]
                {
                    { 1, 60, 10, null, new DateTime(2025, 10, 8, 7, 0, 0, 0, DateTimeKind.Utc), "Standard", "{\"Green\":40, \"Yellow\":5, \"Red\":15}", "Balanced baseline cycle. 40 s green handles moderate mixed traffic." },
                    { 2, 75, 20, null, new DateTime(2025, 10, 8, 17, 0, 0, 0, DateTimeKind.Utc), "Peak", "{\"Green\":50, \"Yellow\":5, \"Red\":20}", "Longer green for heavy traffic periods." },
                    { 3, 50, 0, null, new DateTime(2025, 10, 8, 23, 0, 0, 0, DateTimeKind.Utc), "Night", "{\"Green\":15, \"Yellow\":5, \"Red\":30}", "Short green, long red for minimal traffic." },
                    { 4, 30, 0, null, new DateTime(2025, 10, 8, 10, 0, 0, 0, DateTimeKind.Utc), "Emergency", "{\"Green\":25, \"Yellow\":3, \"Red\":2}", "Immediate priority for emergency vehicles." },
                    { 5, 65, 10, null, new DateTime(2025, 10, 8, 9, 0, 0, 0, DateTimeKind.Utc), "PublicTransport", "{\"Green\":45, \"Yellow\":5, \"Red\":15}", "Extended green for bus approach." },
                    { 6, 40, 0, null, new DateTime(2025, 10, 9, 17, 0, 0, 0, DateTimeKind.Utc), "Pedestrian", "{\"Green\":20, \"Yellow\":5, \"Red\":15}", "Half-cycle pedestrian focus." },
                    { 7, 50, 5, null, new DateTime(2025, 10, 9, 8, 0, 0, 0, DateTimeKind.Utc), "Cyclist", "{\"Green\":30, \"Yellow\":5, \"Red\":15}", "Cycle optimized for bicycle flow." },
                    { 8, 20, 0, null, new DateTime(2025, 10, 9, 18, 0, 0, 0, DateTimeKind.Utc), "Incident", "{\"Green\":0, \"Yellow\":0, \"Red\":20}", "Lock red in case of crash or obstruction." },
                    { 9, 60, 0, null, new DateTime(2025, 10, 10, 12, 0, 0, 0, DateTimeKind.Utc), "Manual", "{\"Green\":20, \"Yellow\":5, \"Red\":35}", "Operator control mode." },
                    { 10, 10, 0, null, new DateTime(2025, 10, 10, 12, 5, 0, 0, DateTimeKind.Utc), "Failover", "{\"Green\":2, \"Yellow\":3, \"Red\":5}", "Flashing yellow fallback mode." }
                });

            migrationBuilder.InsertData(
                table: "traffic_lights",
                columns: new[] { "LightId", "Direction", "IntersectionId", "IsOperational", "Latitude", "LightName", "Longitude" },
                values: new object[,]
                {
                    { 101, "Agiou Spyridonos", 1, true, 38.004685m, "agiou-spyridonos101", 23.676139m },
                    { 102, "Dimitsanas", 1, true, 38.004640m, "dimitsanas102", 23.676094m },
                    { 201, "Anatoliki Pyli", 2, true, 38.003549m, "anatoliki-pyli201", 23.677997m },
                    { 202, "Agiou Spyridonos", 2, true, 38.003570m, "agiou-spyridonos202", 23.678093m },
                    { 301, "Dytiki Pyli", 3, true, 38.002648m, "dytiki-pyli301", 23.674531m },
                    { 302, "Dimitsanas North", 3, true, 38.002696m, "dimitsanas-north302", 23.674498m },
                    { 303, "Dimitsanas South", 3, true, 38.002606m, "dimitsanas-south303", 23.674487m },
                    { 401, "Dimitsanas", 4, true, 38.001626m, "dimitsanas401", 23.673627m },
                    { 402, "Edessis", 4, true, 38.001583m, "edessis402", 23.673566m },
                    { 403, "Korytsas", 4, true, 38.001596m, "korytsas403", 23.673686m },
                    { 501, "Kentriki Pyli", 5, true, 38.004447m, "kentriki-pyli501", 23.676453m },
                    { 502, "Agiou Spyridonos", 5, true, 38.004467m, "agiou-spyridonos502", 23.676528m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_traffic_configurations_IntersectionEntityIntersectionId",
                table: "traffic_configurations",
                column: "IntersectionEntityIntersectionId");

            migrationBuilder.CreateIndex(
                name: "IX_traffic_lights_IntersectionId",
                table: "traffic_lights",
                column: "IntersectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "traffic_configurations");

            migrationBuilder.DropTable(
                name: "traffic_lights");

            migrationBuilder.DropTable(
                name: "intersections");
        }
    }
}
