using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeIngredientNutrition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Calories",
                table: "RecipeIngredients",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsGrams",
                table: "RecipeIngredients",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGrams",
                table: "RecipeIngredients",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGrams",
                table: "RecipeIngredients",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Calories",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "CarbsGrams",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "FatGrams",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "ProteinGrams",
                table: "RecipeIngredients");
        }
    }
}
