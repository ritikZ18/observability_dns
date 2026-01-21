using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ObservabilityDns.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainGroupsAndIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create domain_groups table (if it doesn't exist)
            migrationBuilder.CreateTable(
                name: "domain_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain_groups", x => x.id);
                });

            // Add icon column to domains table (if it doesn't exist)
            migrationBuilder.AddColumn<string>(
                name: "icon",
                table: "domains",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Add group_id column to domains table (if it doesn't exist)
            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "domains",
                type: "uuid",
                nullable: true);

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                        name: "FK_domains_domain_groups_group_id",
                table: "domains",
                column: "group_id",
                        principalTable: "domain_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_domain_groups_enabled",
                table: "domain_groups",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "IX_domain_groups_name",
                table: "domain_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domains_group_id",
                table: "domains",
                column: "group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove indexes
            migrationBuilder.DropIndex(
                name: "IX_domains_group_id",
                table: "domains");

            migrationBuilder.DropIndex(
                name: "IX_domain_groups_name",
                table: "domain_groups");

            migrationBuilder.DropIndex(
                name: "IX_domain_groups_enabled",
                table: "domain_groups");

            // Remove foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_domains_domain_groups_group_id",
                table: "domains");

            // Remove columns from domains table
            migrationBuilder.DropColumn(
                name: "group_id",
                table: "domains");

            migrationBuilder.DropColumn(
                name: "icon",
                table: "domains");

            // Drop domain_groups table
            migrationBuilder.DropTable(
                name: "domain_groups");
        }
    }
}
