using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankweave.Migrations
{
    /// <inheritdoc />
    public partial class AddExcludeFromTotalToFinancialAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromTotal",
                table: "FinancialAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludeFromTotal",
                table: "FinancialAccounts");
        }
    }
}
