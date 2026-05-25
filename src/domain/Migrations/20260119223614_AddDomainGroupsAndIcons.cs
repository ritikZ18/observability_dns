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
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS domain_groups (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500),
    color character varying(7),
    icon character varying(50),
    enabled boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_domain_groups"" PRIMARY KEY (id)
);");

            migrationBuilder.Sql(@"ALTER TABLE domains ADD COLUMN IF NOT EXISTS icon character varying(50);");
            migrationBuilder.Sql(@"ALTER TABLE domains ADD COLUMN IF NOT EXISTS group_id uuid;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_domains_domain_groups_group_id'
    ) THEN
        ALTER TABLE domains
            ADD CONSTRAINT ""FK_domains_domain_groups_group_id""
            FOREIGN KEY (group_id) REFERENCES domain_groups(id) ON DELETE SET NULL;
    END IF;
END $$;");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_domain_groups_enabled"" ON domain_groups (enabled);");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_domain_groups_name"" ON domain_groups (name);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_domains_group_id"" ON domains (group_id);");
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
