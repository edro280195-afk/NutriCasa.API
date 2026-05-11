using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriCasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "favorite_recipes",
                columns: table => new
                {
                    favorite_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorite_recipes", x => x.favorite_id);
                    table.ForeignKey(
                        name: "fk_favorite_recipes_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "recipe_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorite_recipes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_recipes_recipe_id",
                table: "favorite_recipes",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_favorite_recipes_user_id_recipe_id",
                table: "favorite_recipes",
                columns: new[] { "user_id", "recipe_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorite_recipes");
        }
    }
}
