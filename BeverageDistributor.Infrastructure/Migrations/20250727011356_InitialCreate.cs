using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeverageDistributor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Distributors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distributors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistributorAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Complement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DistributorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributorAddresses_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributorContactNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DistributorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorContactNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributorContactNames_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributorPhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DistributorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorPhoneNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributorPhoneNumbers_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributorAddresses_DistributorId_IsMain",
                table: "DistributorAddresses",
                columns: new[] { "DistributorId", "IsMain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributorContactNames_DistributorId_IsPrimary",
                table: "DistributorContactNames",
                columns: new[] { "DistributorId", "IsPrimary" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributorPhoneNumbers_DistributorId",
                table: "DistributorPhoneNumbers",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_Cnpj",
                table: "Distributors",
                column: "Cnpj",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributorAddresses");

            migrationBuilder.DropTable(
                name: "DistributorContactNames");

            migrationBuilder.DropTable(
                name: "DistributorPhoneNumbers");

            migrationBuilder.DropTable(
                name: "Distributors");
        }
    }
}
