using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankweave.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatIfScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatIfScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SavedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateRangeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Days = table.Column<int>(type: "integer", nullable: true),
                    CustomTransactionsJson = table.Column<string>(type: "text", nullable: false),
                    DisabledTransactionsJson = table.Column<string>(type: "text", nullable: false),
                    StatsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatIfScenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatIfScenarios_SavedDate",
                table: "WhatIfScenarios",
                column: "SavedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatIfScenarios");
        }
    }
}
