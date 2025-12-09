using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankweave.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedFieldsForAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomTransactionsJsonEncrypted",
                table: "WhatIfScenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEncrypted",
                table: "WhatIfScenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabledTransactionsJsonEncrypted",
                table: "WhatIfScenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEncrypted",
                table: "WhatIfScenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatsJsonEncrypted",
                table: "WhatIfScenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayNameEncrypted",
                table: "FinancialAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternEncrypted",
                table: "CategorizationRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstNameEncrypted",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameEncrypted",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomTransactionsJsonEncrypted",
                table: "WhatIfScenarios");

            migrationBuilder.DropColumn(
                name: "DescriptionEncrypted",
                table: "WhatIfScenarios");

            migrationBuilder.DropColumn(
                name: "DisabledTransactionsJsonEncrypted",
                table: "WhatIfScenarios");

            migrationBuilder.DropColumn(
                name: "NameEncrypted",
                table: "WhatIfScenarios");

            migrationBuilder.DropColumn(
                name: "StatsJsonEncrypted",
                table: "WhatIfScenarios");

            migrationBuilder.DropColumn(
                name: "DisplayNameEncrypted",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "PatternEncrypted",
                table: "CategorizationRules");

            migrationBuilder.DropColumn(
                name: "FirstNameEncrypted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastNameEncrypted",
                table: "AspNetUsers");
        }
    }
}
