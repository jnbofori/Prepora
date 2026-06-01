using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeNutrition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Calories",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesPerServing",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsGrams",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsGramsPerServing",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGrams",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGramsPerServing",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NutritionCalculatedUtc",
                table: "Recipes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGrams",
                table: "Recipes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGramsPerServing",
                table: "Recipes",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Calories",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CaloriesPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CarbsGrams",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CarbsGramsPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "FatGrams",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "FatGramsPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "NutritionCalculatedUtc",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ProteinGrams",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ProteinGramsPerServing",
                table: "Recipes");
        }
    }
}
