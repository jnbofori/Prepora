using RecipePhotoCommands = Application.Commands.RecipePhotos;
using RecipeCommands = Application.Commands.Recipes;
using Application.DTOs.Nutrition;
using Application.DTOs.Recipes;
using Application.Queries.Recipes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
  public class RecipesController : BaseApiController
  {
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RecipeParams recipeParams)
    {
      return HandlePagedResult(await Mediator.Send(new List.Query { Params = recipeParams }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
      return HandleResult(await Mediator.Send(new Details.Query { Id = id }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RecipeUpsertDto recipe)
    {
      return HandleResult(await Mediator.Send(new RecipeCommands.Create.Command { Recipe = recipe }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, RecipeUpsertDto recipe)
    {
      return HandleResult(await Mediator.Send(new RecipeCommands.Edit.Command { Id = id, Recipe = recipe }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
      return HandleResult(await Mediator.Send(new RecipeCommands.Delete.Command { Id = id }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id)
    {
      return HandleResult(await Mediator.Send(new RecipeCommands.Duplicate.Command { Id = id }));
    }

    [HttpPost("nutrition/calculate")]
    public async Task<IActionResult> CalculateNutrition([FromBody] CalculateNutritionRequest body)
    {
      return HandleResult(await Mediator.Send(new RecipeCommands.CalculateNutrition.Command { Request = body }));
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportPreview([FromBody] ImportUrlRequest body)
    {
      if (body == null) return BadRequest("Request body is required");
      return HandleResult(await Mediator.Send(new RecipeCommands.ImportPreview.Command { Url = body.Url }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> AddPhoto(Guid id, [FromForm] IFormFile file)
    {
      return HandleResult(await Mediator.Send(new RecipePhotoCommands.Add.Command { RecipeId = id, File = file }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpPost("{id:guid}/photos/batch")]
    public async Task<IActionResult> AddPhotos(Guid id, [FromForm] List<IFormFile> files)
    {
      return HandleResult(await Mediator.Send(new RecipePhotoCommands.AddMany.Command { RecipeId = id, Files = files }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpDelete("{id:guid}/photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(Guid id, string photoId)
    {
      return HandleResult(await Mediator.Send(new RecipePhotoCommands.Delete.Command { RecipeId = id, PhotoId = photoId }));
    }

    [Authorize(Policy = "IsRecipeOwner")]
    [HttpPost("{id:guid}/photos/{photoId}/setMain")]
    public async Task<IActionResult> SetMainPhoto(Guid id, string photoId)
    {
      return HandleResult(await Mediator.Send(new RecipePhotoCommands.SetMain.Command { RecipeId = id, PhotoId = photoId }));
    }
  }
}
