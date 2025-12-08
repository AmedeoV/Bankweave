using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankweave.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedFieldsToMoneyMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountEncrypted",
                table: "MoneyMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryEncrypted",
                table: "MoneyMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyNameEncrypted",
                table: "MoneyMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEncrypted",
                table: "MoneyMovements",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountEncrypted",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "CategoryEncrypted",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "CounterpartyNameEncrypted",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "DescriptionEncrypted",
                table: "MoneyMovements");
        }
    }
}
