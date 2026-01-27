using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockPayloadAndMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadData",
                table: "Blocks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Blocks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayloadData",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Blocks");
        }
    }
}
