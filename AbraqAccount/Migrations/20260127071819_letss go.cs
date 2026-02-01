using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbraqAccount.Migrations
{
    /// <inheritdoc />
    public partial class letssgo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty to bypass "Table already exists" errors.
            // The database already has these tables, but __EFMigrationsHistory is missing this migration.
            // We allow EF to mark this as applied without re-running the logic.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
