using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chains",
                columns: table => new
                {
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChainName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    GenesisBlockId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LatestBlockId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chains", x => x.ChainId);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    BlockId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CurrentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Signature = table.Column<string>(type: "TEXT", nullable: false),
                    Nonce = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.BlockId);
                    table.ForeignKey(
                        name: "FK_Blocks_Chains_ChainId",
                        column: x => x.ChainId,
                        principalTable: "Chains",
                        principalColumn: "ChainId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffChainData",
                columns: table => new
                {
                    DataId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptedPayload = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffChainData", x => x.DataId);
                    table.ForeignKey(
                        name: "FK_OffChainData_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "BlockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BlockHeight",
                table: "Blocks",
                column: "BlockHeight");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ChainId",
                table: "Blocks",
                column: "ChainId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ChainId_BlockHeight",
                table: "Blocks",
                columns: new[] { "ChainId", "BlockHeight" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Timestamp",
                table: "Blocks",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OffChainData_BlockId",
                table: "OffChainData",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_OffChainData_CreatedAt",
                table: "OffChainData",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OffChainData_DataType",
                table: "OffChainData",
                column: "DataType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffChainData");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Chains");
        }
    }
}
