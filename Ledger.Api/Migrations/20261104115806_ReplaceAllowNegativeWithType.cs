using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAllowNegativeWithType : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "type",
            table: "accounts",
            type: "text",
            nullable: false,
            defaultValue: "Customer");

        migrationBuilder.Sql(
            "UPDATE accounts SET type = 'System' WHERE allow_negative = true;");

        migrationBuilder.DropColumn(
            name: "allow_negative",
            table: "accounts");
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_negative",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE accounts SET allow_negative = true WHERE type = 'System';");

            migrationBuilder.DropColumn(
                name: "type",
                table: "accounts");
        }
    }
}
