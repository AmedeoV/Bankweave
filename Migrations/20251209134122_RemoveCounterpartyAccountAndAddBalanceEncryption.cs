using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankweave.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCounterpartyAccountAndAddBalanceEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CounterpartyAccount",
                table: "MoneyMovements");

            migrationBuilder.AddColumn<string>(
                name: "AccountBalancesEncrypted",
                table: "BalanceSnapshots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountBalancesEncrypted",
                table: "BalanceSnapshots");

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyAccount",
                table: "MoneyMovements",
                type: "text",
                nullable: true);
        }
    }
}
